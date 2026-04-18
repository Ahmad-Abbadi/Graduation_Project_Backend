using System.Text.Json;

namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class MallSetting
    {
        public Guid Id { get; set; }
        public Guid MallID { get; set; }
        public JsonDocument? OperatingHours { get; set; }
        public JsonDocument? ContactInfo { get; set; }
        public string? ParkingInfo { get; set; }
        public JsonDocument? Services { get; set; }
        public JsonDocument? LoyaltyPointsConfig { get; set; }
        public JsonDocument? NotificationSettings { get; set; }
        public string? MapImageUrl { get; set; }
        public string? LogoUrl { get; set; }
        public JsonDocument? ThemeColors { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
