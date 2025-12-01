using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Net;

public class ContactController : Controller
{
    public class ContactFormDto
    {
        [Required]
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Message { get; set; } = "";
    }

    [HttpPost]
    public IActionResult SendEmail([FromBody] ContactFormDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest("Перевірте правильність введених даних");

        try
        {
            var mail = new MailMessage();
            mail.To.Add("diplomka41ip@gmail.com");
            mail.Subject = "Зворотній зв’язок із сайту";
            mail.Body = $"Ім’я: {data.Name}\nEmail: {data.Email}\nПовідомлення:\n{data.Message}";
            mail.From = new MailAddress("diplomka41ip@gmail.com");

            using var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("diplomka41ip@gmail.com", "vnbz usun rcwr etpy"),
                EnableSsl = true,
            };

            smtp.Send(mail);
            return Ok("Надіслано успішно");
        }
        catch (SmtpException smtpEx)
        {
            // SMTP винятки — найпоширеніші
            return StatusCode(500, $"SMTP-помилка: {smtpEx.Message}\n\n📌 Можливі причини:\n" +
                "- Неправильний SMTP-сервер або порт\n" +
                "- Невірний логін або пароль\n" +
                "- Пошта блокує вхід (використай пароль застосунку, якщо це Gmail)\n" +
                "- Проблеми з SSL або брандмауер блокує порт");
        }
        catch (FormatException fmtEx)
        {
            // Наприклад: некоректна email-адреса у From/To
            return StatusCode(500, $"Формат помилки: {fmtEx.Message}\n\n📌 Перевір email-адреси у полі From або To");
        }
        catch (Exception ex)
        {
            // Загальні помилки — все інше
            return StatusCode(500, $"Невідома помилка: {ex.Message}\n\n📌 Можливі причини:\n" +
                "- Немає з’єднання з інтернетом\n" +
                "- Сервер пошти недоступний\n" +
                "- Внутрішня помилка в .NET");
        }
    }
}
