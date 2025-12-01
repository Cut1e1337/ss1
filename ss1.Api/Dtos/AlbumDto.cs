using System;

namespace ss1.Dtos
{
    public class AlbumDto
    {
        /// <summary>
        /// Id. Для створення можна не заповнювати.
        /// </summary>
        public int? Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string OwnerEmail { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        public string? CoverUrl { get; set; }

        // Для GET — заповнюємо, для POST/PUT можна ігнорити
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Кількість фото в альбомі (тільки для GET)
        /// </summary>
        public int? PhotosCount { get; set; }
    }
}
