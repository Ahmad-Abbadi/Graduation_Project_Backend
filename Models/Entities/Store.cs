using System.Text.Json;

namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Store
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid MallID { get; set; }
        public string? OperatingHours { get; set; }
        public JsonDocument? SocialMediaLinks { get; set; }
        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? FloorNumber { get; set; }
        public string? StoreImageUrl { get; set; }
    }
}
