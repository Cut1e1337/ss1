using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ss1.Api.Dtos;
using ss1.Data;
using ss1.Models;

namespace ss1.Tests
{
    public class SubscriptionsApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public SubscriptionsApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            SeedData(db);
        }

        private void SeedData(ApplicationDbContext db)
        {
            var now = DateTime.UtcNow;

            var user1 = new AppUser
            {
                Email = "user1@example.com",
                PasswordHash = "hash",
                Role = "User",
                FirstName = "User",
                LastName = "One",
                PhoneNumber = "111",
                RegisteredAt = now.AddDays(-30)
            };

            var user2 = new AppUser
            {
                Email = "user2@example.com",
                PasswordHash = "hash",
                Role = "User",
                FirstName = "User",
                LastName = "Two",
                PhoneNumber = "222",
                RegisteredAt = now.AddDays(-20)
            };

            db.Users.AddRange(user1, user2);

            db.Subscriptions.AddRange(
                // Активна підписка user1
                new Subscription
                {
                    UserEmail = "user1@example.com",
                    PlanName = "Pro",
                    AutoRenew = true,
                    StartDate = now.AddDays(-10),
                    EndDate = now.AddDays(20),
                    IsActive = true,
                    CreatedAt = now.AddDays(-10)
                },
                // Старіша неактивна user1
                new Subscription
                {
                    UserEmail = "user1@example.com",
                    PlanName = "Basic",
                    AutoRenew = false,
                    StartDate = now.AddMonths(-2),
                    EndDate = now.AddMonths(-1),
                    IsActive = false,
                    CreatedAt = now.AddMonths(-2)
                },
                // user2 без активної підписки
                new Subscription
                {
                    UserEmail = "user2@example.com",
                    PlanName = "Basic",
                    AutoRenew = false,
                    StartDate = now.AddMonths(-2),
                    EndDate = now.AddDays(-1),
                    IsActive = false,
                    CreatedAt = now.AddMonths(-2)
                }
            );

            db.SaveChanges();
        }

        // 1. GetAll повертає всі підписки
        [Fact]
        public async Task GetAll_ReturnsSeededSubscriptions()
        {
            var response = await _client.GetAsync("/api/subscriptions");
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<SubscriptionDto>>();

            Assert.NotNull(items);
            Assert.True(items!.Count >= 3);
        }

        // 2. GetByUser повертає тільки підписки конкретного користувача
        [Fact]
        public async Task GetByUser_ReturnsOnlyUserSubscriptions()
        {
            var items = await _client.GetFromJsonAsync<List<SubscriptionDto>>(
                "/api/subscriptions/by-user/user1@example.com");

            Assert.NotNull(items);
            Assert.True(items!.Count >= 2);
            Assert.All(items!, x => Assert.Equal("user1@example.com", x.UserEmail));
        }

        // 3. GetCurrent повертає активну підписку
        [Fact]
        public async Task GetCurrent_ReturnsActiveSubscription()
        {
            var response = await _client.GetAsync("/api/subscriptions/by-user/user1@example.com/current");
            response.EnsureSuccessStatusCode();

            var sub = await response.Content.ReadFromJsonAsync<SubscriptionDto>();

            Assert.NotNull(sub);
            Assert.Equal("user1@example.com", sub!.UserEmail);
            Assert.True(sub.IsActive);
            Assert.Equal("Pro", sub.PlanName);
        }

        // 4. GetCurrent для користувача без активної підписки -> 404
        [Fact]
        public async Task GetCurrent_NoActiveSubscription_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/subscriptions/by-user/user2@example.com/current");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 5. Create створює нову підписку і її видно в GetByUser
        [Fact]
        public async Task Create_AddsNewSubscription()
        {
            var dto = new SubscriptionDto
            {
                UserEmail = "newuser@example.com",
                PlanName = "Premium",
                AutoRenew = true,
                // StartDate і EndDate можна не задавати — контролер виставить дефолт
            };

            var response = await _client.PostAsJsonAsync("/api/subscriptions", dto);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<SubscriptionDto>();

            Assert.NotNull(created);
            Assert.True(created!.Id.HasValue);
            Assert.Equal("newuser@example.com", created.UserEmail);
            Assert.Equal("Premium", created.PlanName);

            var byUser = await _client.GetFromJsonAsync<List<SubscriptionDto>>(
                "/api/subscriptions/by-user/newuser@example.com");

            Assert.NotNull(byUser);
            Assert.Single(byUser!);
        }

        // 6. Create з дефолтними датами дає період ~1 місяць
        [Fact]
        public async Task Create_DefaultDates_HasAboutOneMonthPeriod()
        {
            var dto = new SubscriptionDto
            {
                UserEmail = "monthuser@example.com",
                PlanName = "Basic",
                AutoRenew = false
                // StartDate, EndDate = default
            };

            var response = await _client.PostAsJsonAsync("/api/subscriptions", dto);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<SubscriptionDto>();

            Assert.NotNull(created);
            Assert.True(created!.StartDate != default);
            Assert.True(created.EndDate != default);

            var diffDays = (created.EndDate - created.StartDate).TotalDays;

            // 1 місяць ~ 30 днів (допускаємо +- трохи)
            Assert.InRange(diffDays, 27, 35);
        }

        // 7. Update змінює план, автооновлення і активність
        [Fact]
        public async Task Update_ChangesSubscriptionData()
        {
            var all = await _client.GetFromJsonAsync<List<SubscriptionDto>>("/api/subscriptions");
            Assert.NotNull(all);
            var first = all!.First();

            var updateDto = new SubscriptionDto
            {
                Id = first.Id,
                UserEmail = first.UserEmail,
                PlanName = "UpdatedPlan",
                AutoRenew = !first.AutoRenew,
                StartDate = first.StartDate,
                EndDate = first.EndDate.AddDays(10),
                IsActive = !first.IsActive,
                CreatedAt = first.CreatedAt
            };

            var response = await _client.PutAsJsonAsync($"/api/subscriptions/{first.Id}", updateDto);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var entity = await db.Subscriptions.SingleAsync(s => s.Id == first.Id!.Value);

            Assert.Equal("UpdatedPlan", entity.PlanName);
            Assert.Equal(updateDto.AutoRenew, entity.AutoRenew);
            Assert.Equal(updateDto.IsActive, entity.IsActive);
            Assert.Equal(updateDto.EndDate, entity.EndDate);
        }

        // 8. Cancel робить підписку неактивною і закінчує її
        [Fact]
        public async Task Cancel_MarksSubscriptionInactiveAndEndsNow()
        {
            int subId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                subId = db.Subscriptions
                    .Where(s => s.UserEmail == "user1@example.com" && s.IsActive)
                    .Select(s => s.Id)
                    .First();
            }

            var response = await _client.PutAsync($"/api/subscriptions/{subId}/cancel", null);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var sub = await db.Subscriptions.FindAsync(subId);

                Assert.NotNull(sub);
                Assert.False(sub!.IsActive);

                // EndDate має бути не в майбутньому (по відношенню до тестового часу)
                Assert.True(sub.EndDate <= DateTime.UtcNow.AddSeconds(5));
            }
        }

        // 9. Delete видаляє підписку
        [Fact]
        public async Task Delete_RemovesSubscription()
        {
            var all = await _client.GetFromJsonAsync<List<SubscriptionDto>>("/api/subscriptions");
            Assert.NotNull(all);
            var id = all!.First().Id!.Value;

            var deleteResponse = await _client.DeleteAsync($"/api/subscriptions/{id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var exists = await db.Subscriptions.AnyAsync(s => s.Id == id);

            Assert.False(exists);
        }

        // 10. GetByUser повертає підписки в порядку StartDate по спаданню
        [Fact]
        public async Task GetByUser_ReturnsSubscriptionsOrderedByStartDateDesc()
        {
            var items = await _client.GetFromJsonAsync<List<SubscriptionDto>>(
                "/api/subscriptions/by-user/user1@example.com");

            Assert.NotNull(items);
            Assert.True(items!.Count >= 2);

            var dates = items.Select(s => s.StartDate).ToList();
            var sorted = dates.OrderByDescending(d => d).ToList();

            Assert.Equal(sorted, dates);
        }
    }
}
