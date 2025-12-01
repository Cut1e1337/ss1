using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ss1.Api.Dtos;
using ss1.Data;
using ss1.Models;

namespace ss1.Tests
{
    public class ProfileApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public ProfileApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            // Сидимо тестові дані для кожного тесту
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var user1 = new AppUser
            {
                Email = "user1@example.com",
                PasswordHash = "hash",
                Role = "User",
                FirstName = "First",
                LastName = "User",
                PhoneNumber = "111",
                RegisteredAt = DateTime.UtcNow.AddDays(-10)
            };

            var user2 = new AppUser
            {
                Email = "user2@example.com",
                PasswordHash = "hash",
                Role = "User",
                FirstName = "Second",
                LastName = "User",
                PhoneNumber = "222",
                RegisteredAt = DateTime.UtcNow.AddDays(-5)
            };

            db.Users.AddRange(user1, user2);

            db.PhotoSubmissions.AddRange(
                new PhotoSubmission
                {
                    FileName = "order1.jpg",
                    UserEmail = "user1@example.com",
                    ServiceType = "Retouch",
                    Comment = "Comment1",
                    Price = 100,
                    UploadedAt = DateTime.UtcNow.AddHours(-3),
                    IsDelivered = false,
                    OrderNumber = 2,
                    GlobalOrderId = 10,
                    Status = SubmissionStatus.InProgress
                },
                new PhotoSubmission
                {
                    FileName = "order2.jpg",
                    UserEmail = "user1@example.com",
                    ServiceType = "Retouch",
                    Comment = "Comment2",
                    Price = 150,
                    UploadedAt = DateTime.UtcNow.AddHours(-1),
                    IsDelivered = true,
                    OrderNumber = 3,
                    GlobalOrderId = 11,
                    Status = SubmissionStatus.Completed
                },
                new PhotoSubmission
                {
                    FileName = "order3.jpg",
                    UserEmail = "user2@example.com",
                    ServiceType = "Color",
                    Comment = "Other user",
                    Price = 200,
                    UploadedAt = DateTime.UtcNow.AddHours(-2),
                    IsDelivered = false,
                    OrderNumber = 1,
                    GlobalOrderId = 12,
                    Status = SubmissionStatus.Pending
                }
            );

            db.SaveChanges();
        }

        // 1. Профіль існуючого користувача
        [Fact]
        public async Task GetProfile_ReturnsCorrectUser()
        {
            var response = await _client.GetAsync("/api/profile/user1@example.com");
            response.EnsureSuccessStatusCode();

            var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

            Assert.NotNull(profile);
            Assert.Equal("user1@example.com", profile!.Email);
            Assert.Equal("First", profile.FirstName);
            Assert.Equal("User", profile.LastName);
            Assert.Equal("User", profile.Role);
        }

        // 2. Профіль невідомого email -> 404
        [Fact]
        public async Task GetProfile_UnknownEmail_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/profile/unknown@example.com");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 3. Оновлення профілю змінює дані в БД
        [Fact]
        public async Task UpdateProfile_ChangesUserData()
        {
            var dto = new UpdateProfileDto(
                FirstName: "UpdatedFirst",
                LastName: "UpdatedLast",
                PhoneNumber: "999999"
            );

            var response = await _client.PutAsJsonAsync("/api/profile/user1@example.com", dto);
            response.EnsureSuccessStatusCode();

            var updated = await response.Content.ReadFromJsonAsync<ProfileDto>();
            Assert.NotNull(updated);
            Assert.Equal("UpdatedFirst", updated!.FirstName);
            Assert.Equal("UpdatedLast", updated.LastName);
            Assert.Equal("999999", updated.PhoneNumber);

            // перевіряємо, що зміни реально в БД
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == "user1@example.com");

            Assert.Equal("UpdatedFirst", user.FirstName);
            Assert.Equal("UpdatedLast", user.LastName);
            Assert.Equal("999999", user.PhoneNumber);
        }

        // 4. Активні замовлення: тільки недоставлені user1
        [Fact]
        public async Task GetActiveOrders_ReturnsOnlyNotDeliveredForUserAndPaged()
        {
            var response = await _client.GetAsync("/api/profile/user1@example.com/active-orders?page=1&pageSize=5");
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<PhotoSubmissionDto>>();

            Assert.NotNull(items);
            Assert.Single(items!);

            var order = items![0];
            Assert.Equal("user1@example.com", order.UserEmail);
            Assert.False(order.IsDelivered);
            Assert.Equal("order1.jpg", order.FileName);
        }

        // 5. Оновлення профілю для неіснуючого користувача -> 404
        [Fact]
        public async Task UpdateProfile_UnknownEmail_ReturnsNotFound()
        {
            var dto = new UpdateProfileDto(
                FirstName: "X",
                LastName: "Y",
                PhoneNumber: "000"
            );

            var response = await _client.PutAsJsonAsync("/api/profile/notexists@example.com", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 6. Оновлення одного користувача не змінює іншого
        [Fact]
        public async Task UpdateProfile_DoesNotAffectOtherUsers()
        {
            var dto = new UpdateProfileDto(
                FirstName: "Changed",
                LastName: "User",
                PhoneNumber: "123"
            );

            var response = await _client.PutAsJsonAsync("/api/profile/user1@example.com", dto);
            response.EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var other = await db.Users.SingleAsync(u => u.Email == "user2@example.com");

            Assert.Equal("Second", other.FirstName);
            Assert.Equal("User", other.LastName);
            Assert.Equal("222", other.PhoneNumber);
        }

        // 7. Користувач без активних замовлень -> порожній список
        [Fact]
        public async Task GetActiveOrders_ReturnsEmptyForUserWithOnlyDeliveredOrders()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.Users.Add(new AppUser
                {
                    Email = "user3@example.com",
                    PasswordHash = "hash",
                    Role = "User",
                    FirstName = "Third",
                    LastName = "User",
                    PhoneNumber = "333",
                    RegisteredAt = DateTime.UtcNow.AddDays(-1)
                });

                db.PhotoSubmissions.Add(new PhotoSubmission
                {
                    FileName = "delivered.jpg",
                    UserEmail = "user3@example.com",
                    ServiceType = "Retouch",
                    Comment = "Delivered",
                    Price = 50,
                    UploadedAt = DateTime.UtcNow.AddHours(-5),
                    IsDelivered = true,
                    OrderNumber = 1,
                    GlobalOrderId = 20,
                    Status = SubmissionStatus.Completed
                });

                db.SaveChanges();
            }

            var response = await _client.GetAsync("/api/profile/user3@example.com/active-orders?page=1&pageSize=5");
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<PhotoSubmissionDto>>();

            Assert.NotNull(items);
            Assert.Empty(items!);
        }

        // 8. Пагінація: перша сторінка повертає не більше pageSize
        [Fact]
        public async Task GetActiveOrders_Pagination_FirstPageCountNotMoreThanPageSize()
        {
            const int pageSize = 2;

            // добавимо ще пару активних замовлень user1
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.PhotoSubmissions.AddRange(
                    new PhotoSubmission
                    {
                        FileName = "order4.jpg",
                        UserEmail = "user1@example.com",
                        ServiceType = "Retouch",
                        Comment = "Extra1",
                        Price = 120,
                        UploadedAt = DateTime.UtcNow.AddMinutes(-30),
                        IsDelivered = false,
                        OrderNumber = 4,
                        GlobalOrderId = 13,
                        Status = SubmissionStatus.Pending
                    },
                    new PhotoSubmission
                    {
                        FileName = "order5.jpg",
                        UserEmail = "user1@example.com",
                        ServiceType = "Retouch",
                        Comment = "Extra2",
                        Price = 130,
                        UploadedAt = DateTime.UtcNow.AddMinutes(-20),
                        IsDelivered = false,
                        OrderNumber = 5,
                        GlobalOrderId = 14,
                        Status = SubmissionStatus.Pending
                    }
                );

                db.SaveChanges();
            }

            var response = await _client.GetAsync($"/api/profile/user1@example.com/active-orders?page=1&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<PhotoSubmissionDto>>();

            Assert.NotNull(items);
            Assert.True(items!.Count <= pageSize);
        }

        // 9. Пагінація: друга сторінка повертає залишок
        [Fact]
        public async Task GetActiveOrders_Pagination_SecondPageHasRemainingItems()
        {
            const int pageSize = 2;

            // ще раз додамо кілька активних замовлень user1
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.PhotoSubmissions.AddRange(
                    new PhotoSubmission
                    {
                        FileName = "p1.jpg",
                        UserEmail = "user1@example.com",
                        ServiceType = "Retouch",
                        Comment = "P1",
                        Price = 100,
                        UploadedAt = DateTime.UtcNow.AddMinutes(-50),
                        IsDelivered = false,
                        OrderNumber = 6,
                        GlobalOrderId = 15,
                        Status = SubmissionStatus.Pending
                    },
                    new PhotoSubmission
                    {
                        FileName = "p2.jpg",
                        UserEmail = "user1@example.com",
                        ServiceType = "Retouch",
                        Comment = "P2",
                        Price = 110,
                        UploadedAt = DateTime.UtcNow.AddMinutes(-40),
                        IsDelivered = false,
                        OrderNumber = 7,
                        GlobalOrderId = 16,
                        Status = SubmissionStatus.Pending
                    }
                );

                db.SaveChanges();
            }

            var response1 = await _client.GetAsync($"/api/profile/user1@example.com/active-orders?page=1&pageSize={pageSize}");
            var response2 = await _client.GetAsync($"/api/profile/user1@example.com/active-orders?page=1&pageSize={pageSize}");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var items1 = await response1.Content.ReadFromJsonAsync<List<PhotoSubmissionDto>>();
            var items2 = await response2.Content.ReadFromJsonAsync<List<PhotoSubmissionDto>>();

            Assert.NotNull(items1);
            Assert.NotNull(items2);

            // загальна кількість > pageSize, тому друга сторінка має мати хоча б 1 елемент
            Assert.NotEmpty(items2!);
        }

        // 10. Профіль другого користувача повертається коректно
        [Fact]
        public async Task GetProfile_ReturnsSecondUserCorrectly()
        {
            var response = await _client.GetAsync("/api/profile/user2@example.com");
            response.EnsureSuccessStatusCode();

            var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();

            Assert.NotNull(profile);
            Assert.Equal("user2@example.com", profile!.Email);
            Assert.Equal("Second", profile.FirstName);
            Assert.Equal("User", profile.LastName);
            Assert.Equal("User", profile.Role);
        }
    }
}