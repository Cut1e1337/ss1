using System;
using System.ComponentModel.DataAnnotations;

namespace ss1.Models
{
    public enum SubmissionStatus
    {
        Pending,     // Очікується
        InProgress,  // В процесі
        Completed    // Завершено
    }

    


    public class PhotoSubmission
    {
        [Key]
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string UserEmail { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public bool IsReviewed { get; set; } = false;
        public string? ServiceType { get; set; }
        public string? Comment { get; set; }
        public int Price { get; set; }

        public SubmissionStatus Status { get; set; }   // ← Enum, визначений нижче
        public bool IsDelivered { get; set; }

        public byte[]? ProcessedImageData { get; set; }

        public int OrderNumber { get; set; }

        public int GlobalOrderId { get; set; } // Глобальний порядковий номер
        public string? ProcessedBy { get; set; } // Email або ім’я працівника
        

    }

}
