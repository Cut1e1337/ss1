using System;
using System.ComponentModel.DataAnnotations;

namespace ss1.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required, EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Назва плану: наприклад, "Basic", "Pro", "Premium"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PlanName { get; set; } = "Basic";

        /// <summary>
        /// Чи буде підписка автоматично продовжуватись
        /// </summary>
        public bool AutoRenew { get; set; } = false;

        /// <summary>
        /// Дата початку підписки
        /// </summary>
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Дата закінчення підписки
        /// </summary>
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        /// <summary>
        /// Системний флаг (можна деактивувати вручну, навіть якщо EndDate в майбутньому)
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
