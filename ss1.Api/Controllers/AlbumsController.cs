using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Data;
using ss1.Dtos;
using ss1.Models;
using ss1.Api.Validation; // 👈 ДОДАЛИ: простір імен для валідаторів

namespace ss1.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // => api/Albums
    public class AlbumsController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // << свою назву контексту

        public AlbumsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================
        //   HELPERS: mapping
        // =======================

        private static AlbumDto MapToDto(Album album)
        {
            return new AlbumDto
            {
                Id = album.Id,
                Title = album.Title,
                Description = album.Description,
                OwnerEmail = album.OwnerEmail,
                IsPublic = album.IsPublic,
                CoverUrl = album.CoverUrl,
                CreatedAt = album.CreatedAt,
                UpdatedAt = album.UpdatedAt,
                PhotosCount = album.Photos?.Count
            };
        }

        private static void ApplyDtoToEntity(AlbumDto dto, Album entity)
        {
            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.OwnerEmail = dto.OwnerEmail;
            entity.IsPublic = dto.IsPublic;
            entity.CoverUrl = dto.CoverUrl;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        // =======================
        //        ACTIONS
        // =======================

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AlbumDto>>> GetAlbums()
        {
            var albums = await _context.Albums
                .Include(a => a.Photos)
                .ToListAsync();

            var result = albums.Select(MapToDto).ToList();

            return Ok(result);
        }

        // GET: api/Albums/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AlbumDto>> GetAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound();

            return Ok(MapToDto(album));
        }

        // POST: api/Albums
        [HttpPost]
        public async Task<ActionResult<AlbumDto>> CreateAlbum([FromBody] AlbumDto dto)
        {
            // 🔍 Використовуємо CreateAlbumValidator
            var errors = CreateAlbumValidator.Validate(dto);
            if (errors.Any())
            {
                return BadRequest(new { errors });
            }

            var album = new Album
            {
                Title = dto.Title,
                Description = dto.Description,
                OwnerEmail = dto.OwnerEmail,
                IsPublic = dto.IsPublic,
                CoverUrl = dto.CoverUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            var result = MapToDto(album);

            return CreatedAtAction(nameof(GetAlbum), new { id = album.Id }, result);
        }

        // PUT: api/Albums/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAlbum(int id, [FromBody] AlbumDto dto)
        {
            // 🔍 Використовуємо UpdateAlbumValidator
            var errors = UpdateAlbumValidator.Validate(dto);
            if (errors.Any())
            {
                return BadRequest(new { errors });
            }

            var album = await _context.Albums
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound();

            ApplyDtoToEntity(dto, album);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound();

            // Якщо потрібно – можна ще обнулити AlbumId в фото
            if (album.Photos != null)
            {
                foreach (var photo in album.Photos)
                {
                    photo.AlbumId = null;
                    photo.Album = null;
                }
            }

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
