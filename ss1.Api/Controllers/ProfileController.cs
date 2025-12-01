using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Api.Dtos;
using ss1.Data;

namespace ss1.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/profile/{email}
        [HttpGet("{email}")]
        public async Task<ActionResult<ProfileDto>> GetProfile(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();

            var dto = new ProfileDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Role,
                user.RegisteredAt
            );

            return Ok(dto);
        }

        // PUT: api/profile/{email}
        [HttpPut("{email}")]
        public async Task<ActionResult<ProfileDto>> UpdateProfile(string email, [FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();

            var result = new ProfileDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Role,
                user.RegisteredAt
            );

            return Ok(result);
        }

        // POST: api/profile
        [HttpPost]
        public async Task<ActionResult<ProfileDto>> CreateProfile([FromBody] CreateProfileDto dto)
        {
            // Перевірка чи email вже є
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("User with this email already exists.");

            var user = new AppUser
            {
                Email = dto.Email,
                PasswordHash = dto.Password, // ❗ Постав сюди хешування пізніше
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Role = "User",
                RegisteredAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = new ProfileDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Role,
                user.RegisteredAt
            );

            return CreatedAtAction(nameof(GetProfile), new { email = user.Email }, result);
        }


        // GET: api/profile/{email}/active-orders?page=1&pageSize=5
        [HttpGet("{email}/active-orders")]
        public async Task<ActionResult<IEnumerable<PhotoSubmissionDto>>> GetActiveOrders(
            string email,
            int page = 1,
            int pageSize = 5)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 5;

            var query = _context.PhotoSubmissions
                .Where(p => p.UserEmail == email && !p.IsDelivered)
                .OrderByDescending(p => p.OrderNumber);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Пишемо дані пагінації в заголовки (прикольно для Postman)
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Current-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            var dtos = items.Select(p => new PhotoSubmissionDto(
                p.Id,
                p.FileName,
                p.UserEmail,
                p.ServiceType,
                p.Comment,
                p.Price,
                p.UploadedAt,
                p.IsDelivered,
                p.OrderNumber,
                p.GlobalOrderId,
                p.Status
            ));

            return Ok(dtos);
        }
    }
}
