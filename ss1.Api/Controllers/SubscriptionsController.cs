using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Api.Dtos;
using ss1.Data;
using ss1.Models;

namespace ss1.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== helpers ====================

        private static SubscriptionDto MapToDto(Subscription s)
        {
            return new SubscriptionDto
            {
                Id = s.Id,
                UserEmail = s.UserEmail,
                PlanName = s.PlanName,
                AutoRenew = s.AutoRenew,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            };
        }

        private static void ApplyDtoToEntity(SubscriptionDto dto, Subscription s)
        {
            s.UserEmail = dto.UserEmail;
            s.PlanName = dto.PlanName;
            s.AutoRenew = dto.AutoRenew;
            s.StartDate = dto.StartDate;
            s.EndDate = dto.EndDate;
            s.IsActive = dto.IsActive;
        }

        private static bool IsCurrentlyActive(Subscription s)
        {
            var now = DateTime.UtcNow;
            return s.IsActive && s.StartDate <= now && s.EndDate >= now;
        }

        // ==================== endpoints ====================

        // GET: api/subscriptions  (наприклад, для адміна)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetAll()
        {
            var subs = await _context.Subscriptions
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(subs.Select(MapToDto).ToList());
        }

        // GET: api/subscriptions/by-user/{email}
        [HttpGet("by-user/{email}")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetByUser(string email)
        {
            var subs = await _context.Subscriptions
                .Where(s => s.UserEmail == email)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            return Ok(subs.Select(MapToDto).ToList());
        }

        // GET: api/subscriptions/by-user/{email}/current
        [HttpGet("by-user/{email}/current")]
        public async Task<ActionResult<SubscriptionDto>> GetCurrent(string email)
        {
            var now = DateTime.UtcNow;

            var sub = await _context.Subscriptions
                .Where(s =>
                    s.UserEmail == email &&
                    s.IsActive &&
                    s.StartDate <= now &&
                    s.EndDate >= now)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (sub == null)
                return NotFound("No active subscription for this user");

            return Ok(MapToDto(sub));
        }


        // POST: api/subscriptions
        // Створення нової підписки
        [HttpPost]
        public async Task<ActionResult<SubscriptionDto>> Create([FromBody] SubscriptionDto dto)
        {
            // Якщо StartDate / EndDate не задані - виставляємо дефолт "місяць"
            var start = dto.StartDate == default ? DateTime.UtcNow : dto.StartDate;
            var end = dto.EndDate == default ? start.AddMonths(1) : dto.EndDate;

            var sub = new Subscription
            {
                UserEmail = dto.UserEmail,
                PlanName = dto.PlanName,
                AutoRenew = dto.AutoRenew,
                StartDate = start,
                EndDate = end,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(sub);
            await _context.SaveChangesAsync();

            var result = MapToDto(sub);

            return CreatedAtAction(nameof(GetCurrent), new { email = sub.UserEmail }, result);
        }

        // PUT: api/subscriptions/{id}
        // Оновлення параметрів підписки (план, дати, автооновлення)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SubscriptionDto dto)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            if (sub == null)
                return NotFound();

            ApplyDtoToEntity(dto, sub);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/subscriptions/{id}/cancel
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            if (sub == null)
                return NotFound();

            sub.IsActive = false;
            sub.EndDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/subscriptions/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sub = await _context.Subscriptions.FindAsync(id);
            if (sub == null)
                return NotFound();

            _context.Subscriptions.Remove(sub);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
