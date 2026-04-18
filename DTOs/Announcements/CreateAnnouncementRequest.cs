using System.ComponentModel.DataAnnotations;

namespace Graduation_Project_Backend.DTOs.Announcements
{
    public sealed class CreateAnnouncementRequest
    {
        public Guid? StoreId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AnnouncementType { get; set; } = "general";

        [MaxLength(50)]
        public string Priority { get; set; } = "normal";

        public bool IsActive { get; set; } = true;
        public bool IsPinned { get; set; }

        [MaxLength(1000)]
        public string? ImageUrl { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }
}
