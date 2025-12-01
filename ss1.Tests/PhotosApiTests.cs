using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ss1.Api.Dtos;
using ss1.Data;
using ss1.Models;

namespace ss1.Tests
{
    public class PhotosApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public PhotosApiTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();

            // Ініціалізуємо InMemory БД однаково для кожного тесту
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            if (!db.Photos.Any())
            {
                db.Photos.Add(new Photo
                {
                    FileName = "test1.jpg",
                    FilePath = "/uploads/test1.jpg",
                    IsReviewed = false,
                    UploadDate = DateTime.UtcNow,
                    UserEmail = "user@example.com",
                    OrderNumber = 1
                });

                db.Photos.Add(new Photo
                {
                    FileName = "test2.jpg",
                    FilePath = "/uploads/test2.jpg",
                    IsReviewed = true,
                    UploadDate = DateTime.UtcNow.AddMinutes(-5),
                    UserEmail = "user@example.com",
                    OrderNumber = 2
                });

                db.SaveChanges();
            }
        }

        // 1
        [Fact]
        public async Task GetAll_ReturnsSeededPhotos()
        {
            var response = await _client.GetAsync("/api/photos");
            response.EnsureSuccessStatusCode();

            var photos = await response.Content.ReadFromJsonAsync<List<PhotoDto>>();

            Assert.NotNull(photos);
            Assert.True(photos!.Count >= 2);
        }

        // 2
        [Fact]
        public async Task GetById_ReturnsNotFound_ForUnknownPhoto()
        {
            var response = await _client.GetAsync("/api/photos/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 3
        [Fact]
        public async Task Create_Then_GetById_Works()
        {
            var dto = new PhotoDto(
                Id: 0,
                FileName: "new.jpg",
                FilePath: "/uploads/new.jpg",
                IsReviewed: false,
                UploadDate: DateTime.UtcNow,
                UserEmail: "newuser@example.com",
                OrderNumber: 3
            );

            var createResponse = await _client.PostAsJsonAsync("/api/photos", dto);
            createResponse.EnsureSuccessStatusCode();

            var created = await createResponse.Content.ReadFromJsonAsync<PhotoDto>();
            Assert.NotNull(created);
            Assert.True(created!.Id > 0);

            var getResponse = await _client.GetAsync($"/api/photos/{created.Id}");
            getResponse.EnsureSuccessStatusCode();

            var fetched = await getResponse.Content.ReadFromJsonAsync<PhotoDto>();
            Assert.NotNull(fetched);
            Assert.Equal("new.jpg", fetched!.FileName);
        }

        // 4
        [Fact]
        public async Task Delete_RemovesPhoto()
        {
            var all = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");
            Assert.NotNull(all);
            Assert.NotEmpty(all!);

            var id = all!.First().Id;

            var deleteResponse = await _client.DeleteAsync($"/api/photos/{id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse = await _client.GetAsync($"/api/photos/{id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // 5. Перевіряємо, що список фото відсортований за Id
        [Fact]
        public async Task GetAll_ReturnsPhotosSortedById()
        {
            var photos = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");

            Assert.NotNull(photos);
            Assert.True(photos!.Count >= 2);

            var ids = photos.Select(p => p.Id).ToList();
            var sorted = ids.OrderBy(x => x).ToList();

            Assert.Equal(sorted, ids);
        }

        // 6. Видалення неіснуючого фото -> 404
        [Fact]
        public async Task Delete_NotExistingPhoto_ReturnsNotFound()
        {
            var response = await _client.DeleteAsync("/api/photos/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 7. Перевіряємо, що Create повертає Created (201) і Location
        [Fact]
        public async Task Create_ReturnsCreatedStatusAndLocationHeader()
        {
            var dto = new PhotoDto(
                Id: 0,
                FileName: "created.jpg",
                FilePath: "/uploads/created.jpg",
                IsReviewed: false,
                UploadDate: DateTime.UtcNow,
                UserEmail: "user2@example.com",
                OrderNumber: 10
            );

            var response = await _client.PostAsJsonAsync("/api/photos", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }

        // 8. Додаємо кілька фото й перевіряємо, що кількість зросла
        [Fact]
        public async Task Create_MultiplePhotos_IncreasesCount()
        {
            var before = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");
            var beforeCount = before!.Count;

            var dto1 = new PhotoDto(0, "multi1.jpg", "/uploads/multi1.jpg", false,
                DateTime.UtcNow, "m1@example.com", 5);
            var dto2 = new PhotoDto(0, "multi2.jpg", "/uploads/multi2.jpg", false,
                DateTime.UtcNow, "m2@example.com", 6);

            await _client.PostAsJsonAsync("/api/photos", dto1);
            await _client.PostAsJsonAsync("/api/photos", dto2);

            var after = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");
            var afterCount = after!.Count;

            Assert.Equal(beforeCount + 2, afterCount);
        }

        // 9. Після видалення кількість фото зменшується
        [Fact]
        public async Task Delete_DecreasesCount()
        {
            var before = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");
            var beforeCount = before!.Count;
            var id = before.First().Id;

            var deleteResponse = await _client.DeleteAsync($"/api/photos/{id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var after = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");
            var afterCount = after!.Count;

            Assert.Equal(beforeCount - 1, afterCount);
        }

        [Fact]
        public async Task GetById_ReturnsExistingPhoto()
        {
            var all = await _client.GetFromJsonAsync<List<PhotoDto>>("/api/photos");
            Assert.NotNull(all);
            Assert.NotEmpty(all!);

            var first = all!.First();

            var response = await _client.GetAsync($"/api/photos/{first.Id}");
            response.EnsureSuccessStatusCode();

            var single = await response.Content.ReadFromJsonAsync<PhotoDto>();

            Assert.NotNull(single);
            Assert.Equal(first.Id, single!.Id);
            Assert.Equal(first.FileName, single.FileName);
            Assert.Equal(first.UserEmail, single.UserEmail);
        }
    }
}