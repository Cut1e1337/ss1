using System.ComponentModel.DataAnnotations;

namespace ss1.Models
{
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Введіть ім’я")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Неправильний формат email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введіть повідомлення")]
        [MaxLength(250, ErrorMessage = "Максимальна довжина — 250 символів")]
        public string Message { get; set; }
    }
}
