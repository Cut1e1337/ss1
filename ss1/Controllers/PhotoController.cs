using Microsoft.AspNetCore.Mvc;
using ss1.Data;
using ss1.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;


namespace ss1.Controllers
{
    public class PhotoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PhotoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Сторінка завантаження
        [HttpGet]
        public IActionResult Upload()
        {
            return View(); // Повертає Upload.cshtml
        }

        // Обробка завантаження файлу
        //[HttpPost]
        //public async Task<IActionResult> Upload(IFormFile file)
        //{
        //    if (file != null && file.Length > 0)
        //    {
        //        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        //        var fileExtension = Path.GetExtension(file.FileName).ToLower();

        //        if (!allowedExtensions.Contains(fileExtension))
        //        {
        //            return Json(new { message = "Дозволено лише файли .jpg, .jpeg, .png, .gif!", isError = true });
        //        }

        //        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
        //        if (!Directory.Exists(uploadsFolder))
        //            Directory.CreateDirectory(uploadsFolder);

        //        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
        //        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        var photo = new Photo
        //        {
        //            FileName = uniqueFileName,
        //            FilePath = Path.Combine("uploads", uniqueFileName).Replace("\\", "/"),
        //            UploadDate = DateTime.UtcNow
        //        };

        //        _context.Photos.Add(photo);
        //        await _context.SaveChangesAsync();

        //        return Json(new { message = "Файл успішно завантажено!", isError = false });
        //    }
        //    else
        //    {
        //        return Json(new { message = "Будь ласка, виберіть файл!", isError = true });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { message = "Дозволено лише файли .jpg, .jpeg, .png, .gif!", isError = true });
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Отримання користувача (email)
                var userEmail = User.Identity.Name ?? "unknown@example.com";

                // Знаходимо останній OrderNumber для цього користувача
                var lastOrderNumber = await _context.Photos
                    .Where(p => p.UserEmail == userEmail)
                    .OrderByDescending(p => p.OrderNumber)
                    .Select(p => p.OrderNumber)
                    .FirstOrDefaultAsync();

                var newOrderNumber = lastOrderNumber + 1;

                var photo = new Photo
                {
                    FileName = uniqueFileName,
                    FilePath = Path.Combine("uploads", uniqueFileName).Replace("\\", "/"),
                    UploadDate = DateTime.UtcNow,
                    UserEmail = userEmail,
                    OrderNumber = newOrderNumber
                };

                _context.Photos.Add(photo);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    message = $"Файл успішно завантажено! Замовлення №{newOrderNumber}",
                    orderNumber = newOrderNumber,
                    isError = false
                });
            }
            else
            {
                return Json(new { message = "Будь ласка, виберіть файл!", isError = true });
            }
        }



        // Галерея фотографій
        public IActionResult AdminGallery()
        {
            var photos = _context.Photos.ToList();
            return View(photos);
        }

        // Видалення фотографії
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
                return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, photo.FilePath);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Фото успішно видалено!";
            return RedirectToAction(nameof(AdminGallery));
        }

        // Завантаження фотографії на клієнт
        public async Task<IActionResult> Download(int id)
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
                return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, photo.FilePath);
            var memory = new MemoryStream();

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }

            memory.Position = 0;
            return File(memory, "application/octet-stream", photo.FileName);
        }
    }
}
