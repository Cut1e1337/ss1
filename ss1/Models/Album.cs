using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ss1.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [EmailAddress]
        public string OwnerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Публічний альбом чи ні
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// URL обкладинки (наприклад, шлях до однієї з фотографій)
        /// </summary>
        public string? CoverUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Фото, що належать альбому (one-to-many)
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }
}
