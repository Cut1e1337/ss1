using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Api.Dtos;
using ss1.Api.Validation;   // 👈 додали
using ss1.Data;
using ss1.Models;

namespace ss1.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PhotosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/photos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhotoDto>>> GetAll()
        {
            var photos = await _context.Photos.ToListAsync();

            var dtos = photos.Select(p => new PhotoDto(
                p.Id,
                p.FileName,
                p.FilePath,
                p.IsReviewed,
                p.UploadDate,
                p.UserEmail,
                p.OrderNumber
            ));

            return Ok(dtos);
        }

        // GET: api/photos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PhotoDto>> GetById(int id)
        {
            var p = await _context.Photos.FindAsync(id);
            if (p == null) return NotFound();

            var dto = new PhotoDto(
                p.Id,
                p.FileName,
                p.FilePath,
                p.IsReviewed,
                p.UploadDate,
                p.UserEmail,
                p.OrderNumber
            );

            return Ok(dto);
        }

        // POST: api/photos
        [HttpPost]
        public async Task<ActionResult<PhotoDto>> Create([FromBody] PhotoDto dto)
        {
            // ✅ валідація
            var errors = CreatePhotoValidator.Validate(dto);
            if (errors.Any())
            {
                return BadRequest(new { errors });
            }

            var photo = new Photo
            {
                FileName = dto.FileName,
                FilePath = dto.FilePath,
                IsReviewed = dto.IsReviewed,
                UploadDate = dto.UploadDate,
                UserEmail = dto.UserEmail,
                OrderNumber = dto.OrderNumber
            };

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            var created = new PhotoDto(
                photo.Id,
                photo.FileName,
                photo.FilePath,
                photo.IsReviewed,
                photo.UploadDate,
                photo.UserEmail,
                photo.OrderNumber
            );

            return CreatedAtAction(nameof(GetById), new { id = photo.Id }, created);
        }

        // DELETE: api/photos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null) return NotFound();

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
