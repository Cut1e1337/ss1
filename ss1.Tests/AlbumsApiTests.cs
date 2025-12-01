using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ss1.Api.Dtos;
using ss1.Data;
using ss1.Dtos;
using ss1.Models;
using System.Net;
using System.Net.Http.Json;

namespace ss1.Tests
{
    public class AlbumsApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AlbumsApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            // Ініціалізація InMemory БД для цього набору тестів
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Сідаємо базові дані
            SeedData(db);
        }

        private void SeedData(ApplicationDbContext db)
        {
            if (db.Albums.Any())
                return;

            var user1 = new AppUser
            {
                Email = "albumuser@example.com",
                PasswordHash = "hash",
                Role = "User",
                FirstName = "Album",
                LastName = "Owner",
                PhoneNumber = "123",
                RegisteredAt = DateTime.UtcNow.AddDays(-10)
            };

            var user2 = new AppUser
            {
                Email = "seconduser@example.com",
                PasswordHash = "hash",
                Role = "User",
                FirstName = "Second",
                LastName = "Owner",
                PhoneNumber = "456",
                RegisteredAt = DateTime.UtcNow.AddDays(-5)
            };

            db.Users.AddRange(user1, user2);

            var album1 = new Album
            {
                Title = "Portfolio",
                Description = "Main works",
                OwnerEmail = "albumuser@example.com",
                IsPublic = true,
                CoverUrl = "/covers/portfolio.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            };

            var album2 = new Album
            {
                Title = "Weddings",
                Description = "Wedding shots",
                OwnerEmail = "albumuser@example.com",
                IsPublic = false,
                CoverUrl = "/covers/weddings.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            db.Albums.AddRange(album1, album2);
            db.SaveChanges();

            var photo1 = new Photo
            {
                FileName = "p1.jpg",
                FilePath = "/uploads/p1.jpg",
                IsReviewed = false,
                UploadDate = DateTime.UtcNow.AddHours(-5),
                UserEmail = "albumuser@example.com",
                OrderNumber = 1,
                AlbumId = album1.Id
            };

            var photo2 = new Photo
            {
                FileName = "p2.jpg",
                FilePath = "/uploads/p2.jpg",
                IsReviewed = true,
                UploadDate = DateTime.UtcNow.AddHours(-4),
                UserEmail = "albumuser@example.com",
                OrderNumber = 2,
                AlbumId = album1.Id
            };

            var photo3 = new Photo
            {
                FileName = "p3.jpg",
                FilePath = "/uploads/p3.jpg",
                IsReviewed = true,
                UploadDate = DateTime.UtcNow.AddHours(-3),
                UserEmail = "seconduser@example.com",
                OrderNumber = 3,
                AlbumId = album2.Id
            };

            db.Photos.AddRange(photo1, photo2, photo3);
            db.SaveChanges();
        }

        // 1. Отримання всіх альбомів повертає seed-дані
        [Fact]
        public async Task GetAll_ReturnsSeededAlbums()
        {
            var response = await _client.GetAsync("/api/albums");
            response.EnsureSuccessStatusCode();

            var albums = await response.Content.ReadFromJsonAsync<List<AlbumDto>>();

            Assert.NotNull(albums);
            Assert.True(albums!.Count >= 2);
        }

        // 2. GetById для неіснуючого альбому -> 404
        [Fact]
        public async Task GetById_ReturnsNotFound_ForUnknownAlbum()
        {
            var response = await _client.GetAsync("/api/albums/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 3. Create + GetById працюють разом
        [Fact]
        public async Task Create_Then_GetById_Works()
        {
            var dto = new AlbumDto
            {
                Title = "New Album",
                Description = "Test description",
                OwnerEmail = "albumuser@example.com",
                IsPublic = true,
                CoverUrl = "/covers/new.jpg"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/albums", dto);
            createResponse.EnsureSuccessStatusCode();

            var created = await createResponse.Content.ReadFromJsonAsync<AlbumDto>();
            Assert.NotNull(created);
            Assert.True(created!.Id.HasValue);
            Assert.True(created.Id!.Value > 0);

            var getResponse = await _client.GetAsync($"/api/albums/{created.Id}");
            getResponse.EnsureSuccessStatusCode();

            var fetched = await getResponse.Content.ReadFromJsonAsync<AlbumDto>();
            Assert.NotNull(fetched);
            Assert.Equal("New Album", fetched!.Title);
            Assert.Equal("albumuser@example.com", fetched.OwnerEmail);
        }

        // 4. Видалення альбому реально його видаляє
        [Fact]
        public async Task Delete_RemovesAlbum()
        {
            var all = await _client.GetFromJsonAsync<List<AlbumDto>>("/api/albums");
            Assert.NotNull(all);
            Assert.NotEmpty(all!);

            var id = all!.First().Id!.Value;

            var deleteResponse = await _client.DeleteAsync($"/api/albums/{id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse = await _client.GetAsync($"/api/albums/{id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // 5. Список альбомів відсортований за Id (якщо ти так зробив у контролері)
        [Fact]
        public async Task GetAll_ReturnsAlbumsSortedById()
        {
            var albums = await _client.GetFromJsonAsync<List<AlbumDto>>("/api/albums");

            Assert.NotNull(albums);
            Assert.True(albums!.Count >= 2);

            var ids = albums!
                .Where(a => a.Id.HasValue)
                .Select(a => a.Id!.Value)
                .ToList();

            var sorted = ids.OrderBy(x => x).ToList();

            Assert.Equal(sorted, ids);
        }

        // 6. Видалення неіснуючого альбому -> 404
        [Fact]
        public async Task Delete_NotExistingAlbum_ReturnsNotFound()
        {
            var response = await _client.DeleteAsync("/api/albums/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 7. Create повертає 201 і Location
        [Fact]
        public async Task Create_ReturnsCreatedStatusAndLocationHeader()
        {
            var dto = new AlbumDto
            {
                Title = "Created Album",
                Description = "Created desc",
                OwnerEmail = "albumuser@example.com",
                IsPublic = true
            };

            var response = await _client.PostAsJsonAsync("/api/albums", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }

        // 8. Створення декількох альбомів збільшує кількість
        [Fact]
        public async Task Create_MultipleAlbums_IncreasesCount()
        {
            var before = await _client.GetFromJsonAsync<List<AlbumDto>>("/api/albums");
            var beforeCount = before!.Count;

            var dto1 = new AlbumDto
            {
                Title = "Multi1",
                OwnerEmail = "albumuser@example.com",
                IsPublic = true
            };

            var dto2 = new AlbumDto
            {
                Title = "Multi2",
                OwnerEmail = "albumuser@example.com",
                IsPublic = false
            };

            await _client.PostAsJsonAsync("/api/albums", dto1);
            await _client.PostAsJsonAsync("/api/albums", dto2);

            var after = await _client.GetFromJsonAsync<List<AlbumDto>>("/api/albums");
            var afterCount = after!.Count;

            Assert.Equal(beforeCount + 2, afterCount);
        }

        // 9. Оновлення альбому змінює дані в БД
        [Fact]
        public async Task Update_ChangesAlbumData()
        {
            var all = await _client.GetFromJsonAsync<List<AlbumDto>>("/api/albums");
            Assert.NotNull(all);
            Assert.NotEmpty(all!);

            var first = all!.First();

            var updateDto = new AlbumDto
            {
                Id = first.Id,
                Title = "Updated Title",
                Description = "Updated Description",
                OwnerEmail = first.OwnerEmail,
                IsPublic = !first.IsPublic,
                CoverUrl = first.CoverUrl
            };

            var response = await _client.PutAsJsonAsync($"/api/albums/{first.Id}", updateDto);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var updatedResponse = await _client.GetAsync($"/api/albums/{first.Id}");
            updatedResponse.EnsureSuccessStatusCode();

            var updated = await updatedResponse.Content.ReadFromJsonAsync<AlbumDto>();
            Assert.NotNull(updated);
            Assert.Equal("Updated Title", updated!.Title);
            Assert.Equal("Updated Description", updated.Description);
            Assert.Equal(!first.IsPublic, updated.IsPublic);
        }

        // 10. PhotosCount у DTO відповідає реальній кількості фото в БД
        [Fact]
        public async Task GetAlbum_PhotosCountMatchesDb()
        {
            int albumId;
            int expectedCount;

            // дістаємо реальні дані з БД
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var album = db.Albums
                    .Include(a => a.Photos)
                    .First();

                albumId = album.Id;
                expectedCount = album.Photos.Count;
            }

            var response = await _client.GetAsync($"/api/albums/{albumId}");
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<AlbumDto>();
            Assert.NotNull(dto);
            Assert.Equal(expectedCount, dto!.PhotosCount);
        }
    }
}
