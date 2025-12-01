using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Data;
using ss1.Models;
using System;


namespace ss1.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> ActiveOrdersPartial(int page = 1)
        {
            var email = User.Identity?.Name;
            int pageSize = 5;

            var orders = await _context.PhotoSubmissions
                .Where(p => p.UserEmail == email && !p.IsDelivered)
                .OrderByDescending(p => p.OrderNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalCount = await _context.PhotoSubmissions
                .CountAsync(p => p.UserEmail == email && !p.IsDelivered);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return PartialView("_ActiveOrdersPartial", orders);
        }


        [Authorize]
        public async Task<IActionResult> Index(int page = 1)
        {
            var email = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToAction("Login", "Account");

            int pageSize = 3;
            var query = _context.PhotoSubmissions
                .Where(p => p.UserEmail == email && !p.IsDelivered)
                .OrderBy(p => p.OrderNumber);

            var totalCount = await query.CountAsync();
            var activeOrders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.ActiveOrders = activeOrders;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View(user);
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Update(AppUser model)
        {
            var email = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            await _context.SaveChangesAsync();

            ViewBag.Message = "Дані оновлено";
            return View("Index", user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadPhoto(List<IFormFile> photoFile, string serviceType, string comment, int price)
        {
            if (photoFile == null || photoFile.Count == 0)
            {
                TempData["UploadMessage"] = "Файли не вибрано.";
                return RedirectToAction("Index");
            }

            var userEmail = User.Identity?.Name ?? "Невідомо";

            // ✅ Отримуємо останній глобальний номер (глобальний ID)
            int lastGlobalId = await _context.PhotoSubmissions
                .OrderByDescending(p => p.GlobalOrderId)
                .Select(p => p.GlobalOrderId)
                .FirstOrDefaultAsync();

            int nextGlobalId = lastGlobalId + 1; // ✅ Створюємо новий номер

            // ✅ Визначаємо локальний номер для користувача
            var lastOrder = await _context.PhotoSubmissions
                .Where(p => p.UserEmail == userEmail)
                .OrderByDescending(p => p.OrderNumber)
                .FirstOrDefaultAsync();

            int newOrderNumber = (lastOrder?.OrderNumber ?? 0) + 1;

            foreach (var file in photoFile)
            {
                if (file.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var imageData = ms.ToArray();

                    var submission = new PhotoSubmission
                    {
                        FileName = file.FileName,
                        ImageData = imageData,
                        UserEmail = userEmail,
                        ServiceType = serviceType,
                        Comment = comment,
                        Price = price,
                        UploadedAt = DateTime.UtcNow,
                        OrderNumber = newOrderNumber,
                        GlobalOrderId = nextGlobalId, // 🔥 Глобальний ID
                        Status = SubmissionStatus.Pending
                    };

                    _context.PhotoSubmissions.Add(submission);
                }
            }

            await _context.SaveChangesAsync();

            TempData["UploadMessage"] = $"Фото ({photoFile.Count}) надіслано на обробку. Замовлення №{nextGlobalId}. Статус: Очікується.";
            return RedirectToAction("Index");
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0) return RedirectToAction("Index");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            using var ms = new MemoryStream();
            await avatar.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            user.AvatarImage = ResizeImageToSquare(imageBytes, 256, 256); // форматування

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        private byte[] ResizeImageToSquare(byte[] imageData, int width, int height)
        {
            using var inputStream = new MemoryStream(imageData);
            using var originalImage = System.Drawing.Image.FromStream(inputStream);
            using var resizedBitmap = new System.Drawing.Bitmap(width, height);
            using var graphics = System.Drawing.Graphics.FromImage(resizedBitmap);
            graphics.DrawImage(originalImage, 0, 0, width, height);

            using var ms = new MemoryStream();
            resizedBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
    }
}
