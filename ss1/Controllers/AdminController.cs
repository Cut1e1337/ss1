using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Data;
using ss1.Models;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Utils;
using MailKit.Net.Smtp;
using MailKit.Security;


namespace ss1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }


        public IActionResult Dashboard()
        {
            var totalUsers = _context.Users.Count();
            var admins = _context.Users.Count(u => u.Role == "Admin");
            var users = _context.Users.Count(u => u.Role == "User");

            var totalSubmissions = _context.PhotoSubmissions.Count(); // ← правильно
            var reviewed = _context.PhotoSubmissions.Count(p => p.IsReviewed);

            var model = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                Admins = admins,
                RegularUsers = users,
                TotalSubmissions = totalSubmissions,
                ReviewedSubmissions = reviewed
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            var total = _context.Users.Count();
            var admins = _context.Users.Count(u => u.Role == "Admin");
            var users = _context.Users.Count(u => u.Role == "User");
            var submissions = _context.PhotoSubmissions.Count();
            var reviewed = _context.PhotoSubmissions.Count(p => p.IsReviewed);

            return Json(new
            {
                totalUsers = total,
                admins,
                users,
                submissions,
                reviewed
            });
        }


        public IActionResult Users()
        {
            var users = _context.Users.ToList(); // або AsNoTracking()
            return View(users); // Views/Admin/Users.cshtml
        }


        public IActionResult _Layout()
        {
            return View(); // Views/Admin/Users.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> ViewProcessedPhoto(int id)
        {
            var submission = await _context.PhotoSubmissions.FindAsync(id);
            if (submission == null || submission.ProcessedImageData == null)
                return NotFound();

            return File(submission.ProcessedImageData, "image/jpeg");
        }


        public async Task<IActionResult> Orders()
        {
            var submissions = await _context.PhotoSubmissions.ToListAsync();

            ViewBag.ServiceNames = new Dictionary<string, string>
    {
        { "background-removal", "Видалення фону" },
        { "color-correction", "Корекція кольору" },
        { "retouch", "Ретуш" },
        { "convert-bw", "Чорно-біле зображення" }
    };

            return View(submissions);
        }

        [HttpPost]
        public async Task<IActionResult> UploadProcessedPhoto(int id, IFormFile processedFile)
        {
            if (processedFile == null || processedFile.Length == 0)
                return BadRequest("Файл не вибрано");

            var submission = await _context.PhotoSubmissions.FindAsync(id);
            if (submission == null)
                return NotFound();

            using var ms = new MemoryStream();
            await processedFile.CopyToAsync(ms);
            int lastGlobalId = await _context.PhotoSubmissions
                .OrderByDescending(p => p.GlobalOrderId)
                .Select(p => p.GlobalOrderId)
                .FirstOrDefaultAsync();

            int nextGlobalId = lastGlobalId + 1;


            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }


        public async Task<IActionResult> DownloadProcessedPhoto(int id)
        {
            var submission = await _context.PhotoSubmissions.FindAsync(id);

            if (submission == null || submission.ProcessedImageData == null)
                return NotFound();

            return File(submission.ProcessedImageData, "image/jpeg", $"processed_{submission.FileName}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAndSendProcessedPhoto(int id, IFormFile processedFile)
        {
            if (processedFile == null || processedFile.Length == 0)
                return BadRequest("Файл не вибрано");

            var submission = await _context.PhotoSubmissions.FindAsync(id);
            if (submission == null)
                return NotFound();

            using var ms = new MemoryStream();
            await processedFile.CopyToAsync(ms);
            submission.ProcessedImageData = ms.ToArray();
            submission.ProcessedBy = User.Identity?.Name;

            // Надсилання email
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Photo Studio", "diplomka41ip@gmail.com"));
            message.To.Add(new MailboxAddress("", submission.UserEmail));
            message.Subject = "Ваше оброблене фото готове!";

            var builder = new BodyBuilder
            {
                HtmlBody = "<p>Ваше фото оброблено. Воно у вкладенні.</p>"
            };

            builder.Attachments.Add($"processed_{submission.FileName}", submission.ProcessedImageData, new ContentType("image", "jpeg"));
            message.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("diplomka41ip@gmail.com", "vnbz usun rcwr etpy");
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            // Позначаємо як доставлене
            submission.IsDelivered = true;

            submission.ProcessedImageData = ms.ToArray();
            submission.IsDelivered = true;
            submission.ProcessedBy = User.Identity?.Name; // ← ДОДАЙ ЦЕ
            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }



        [HttpGet]
        public async Task<IActionResult> Archive()
        {
            var submissions = await _context.PhotoSubmissions
                .Where(p => p.IsDelivered)
                .OrderByDescending(p => p.UploadedAt)
                .ToListAsync();

            ViewBag.ServiceNames = new Dictionary<string, string>
    {
        { "background-removal", "Видалення фону" },
        { "color-correction", "Корекція кольору" },
        { "retouch", "Ретуш" },
        { "convert-bw", "Чорно-біле зображення" }
    };

            return View(submissions);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] StatusUpdateModel model)
        {
            if (!Enum.TryParse<SubmissionStatus>(model.NewStatus, out var parsedStatus))
                return BadRequest("Invalid status");

            var submission = await _context.PhotoSubmissions.FindAsync(model.Id);
            if (submission == null)
                return NotFound();

            submission.Status = parsedStatus;
            await _context.SaveChangesAsync();

            return Ok();
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SendToClient(int id)
        //{
        //    var submission = await _context.PhotoSubmissions.FindAsync(id);
        //    if (submission != null)
        //    {
        //        submission.IsDelivered = true;
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction("Orders");
        //}



        public IActionResult Products()
        {
            return View(); // Views/Admin/_Products.cshtml — теж перейменуй
        }

        public IActionResult Messages()
        {
            return View(); // Views/Admin/_Messages.cshtml — перейменуй
        }

        public IActionResult Settings()
        {
            return View(); // Views/Admin/_Settings.cshtml — перейменуй
        }

        //public IActionResult ReviewPhotos()
        //{
        //    return View(); // Views/Admin/ReviewPhotos.cshtml
        //}

        //public async Task<IActionResult> ReviewPhotos()
        //{
        //    var photos = await _context.PhotoSubmissions
        //        .Where(p => !p.IsReviewed)
        //        .OrderByDescending(p => p.UploadedAt)
        //        .ToListAsync();

        //    return View(photos);
        //}

        public async Task<IActionResult> ReviewPhotos()
        {
            var submissions = await _context.PhotoSubmissions
                .Where(p => !p.IsReviewed)
                .OrderByDescending(p => p.UploadedAt)
                .Select(p => new PhotoSubmission
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    UserEmail = p.UserEmail,
                    ServiceType = p.ServiceType,
                    Comment = p.Comment,
                    Price = p.Price,
                    UploadedAt = p.UploadedAt,
                    IsReviewed = p.IsReviewed,
                    OrderNumber = p.OrderNumber,
                    GlobalOrderId = p.GlobalOrderId, 
                    ProcessedBy = p.ProcessedBy       
                })
                .ToListAsync();

            ViewBag.ServiceNames = new Dictionary<string, string>
    {
        { "background-removal", "Видалення фону" },
        { "color-correction", "Корекція кольору" },
        { "retouch", "Ретуш" },
        { "convert-bw", "Чорно-біле зображення" }
    };

            return View(submissions);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPhoto(int id)
        {
            var photo = await _context.PhotoSubmissions
                .FirstOrDefaultAsync(p => p.Id == id);

            if (photo == null || photo.ImageData == null)
                return NotFound();

            return File(photo.ImageData, "image/jpeg", photo.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> ViewPhoto(int id)
        {
            var photo = await _context.PhotoSubmissions
                .FirstOrDefaultAsync(p => p.Id == id);

            if (photo == null || photo.ImageData == null)
                return NotFound();

            return File(photo.ImageData, "image/jpeg"); // без третього параметра — браузер просто відобразить
        }





        //[HttpPost]
        //public async Task<IActionResult> MarkReviewed(int id)
        //{
        //    var photo = await _context.PhotoSubmissions.FindAsync(id);
        //    if (photo != null)
        //    {
        //        photo.IsReviewed = true;
        //        await _context.SaveChangesAsync();
        //        _context.SaveChanges();

        //    }

        //    return RedirectToAction("ReviewPhotos");
        //}

        [HttpPost]
        public async Task<IActionResult> MarkReviewed(int id)
        {
            var photo = await _context.PhotoSubmissions.FindAsync(id);
            if (photo != null)
            {
                photo.IsReviewed = true;
                await _context.SaveChangesAsync();
                _context.SaveChanges();

            }

            return RedirectToAction("ReviewPhotos");
        }

    }
}
