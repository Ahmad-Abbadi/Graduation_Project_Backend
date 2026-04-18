using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Graduation_Project_Backend.DTOs.Stores
{
    public sealed class CreateStoreRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? OperatingHours { get; set; }

        public JsonElement? SocialMediaLinks { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? FloorNumber { get; set; }

        [MaxLength(1000)]
        public string? StoreImageUrl { get; set; }

        public List<long>? CategoryIds { get; set; }
    }
}
