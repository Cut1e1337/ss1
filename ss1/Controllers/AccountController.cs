using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ss1.Data;
using ss1.Interfaces;
using ss1.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ss1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ConfirmEmail()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            // Хешування SHA256 — просте, без солі
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Користувач з таким email вже існує.";
                return View();
            }

            var confirmationCode = Guid.NewGuid().ToString().Substring(0, 6);

            var user = new AppUser
            {
                Email = email,
                PasswordHash = HashPassword(password),
                EmailConfirmed = false,
                EmailConfirmationCode = confirmationCode,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(email, "Код підтвердження пошти",
                $"Ваш код підтвердження: <b>{confirmationCode}</b>");

            TempData["Email"] = email;
            ViewBag.Message = "Реєстрація успішна. Перевірте пошту.";
            return RedirectToAction("ConfirmEmail");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.EmailConfirmed)
            {
                ViewBag.Error = "Користувача не знайдено або пошта не підтверджена.";
                ViewBag.ResendLink = Url.Action("ResendConfirmation", "Account", new { email });
                return View();
            }


            var hashedPassword = HashPassword(password);

            if (user.PasswordHash != hashedPassword)
            {
                ViewBag.Error = "Невірний пароль.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe
                });

            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(string email, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Користувача не знайдено.";
                return View();
            }

            if (user.EmailConfirmationCode != code)
            {
                ViewBag.Error = "Невірний код.";
                return View();
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationCode = null;
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Користувача не знайдено.";
                return RedirectToAction("Login");
            }

            if (user.EmailConfirmed)
            {
                ViewBag.Message = "Пошта вже підтверджена.";
                return RedirectToAction("Login");
            }

            var newCode = Guid.NewGuid().ToString().Substring(0, 6);
            user.EmailConfirmationCode = newCode;
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(user.Email, "Новий код підтвердження",
                $"Ваш новий код підтвердження: <b>{newCode}</b>");

            TempData["Email"] = user.Email;
            TempData["Info"] = "Новий код надіслано на вашу пошту.";
            return RedirectToAction("ConfirmEmail");
        }

    }
}
