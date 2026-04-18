using System.Text.Json;

namespace Graduation_Project_Backend.DTOs.Stores
{
    public sealed class StoreResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public Guid MallID { get; init; }
        public string? OperatingHours { get; init; }
        public JsonElement? SocialMediaLinks { get; init; }
        public string? Description { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Email { get; init; }
        public string? FloorNumber { get; init; }
        public string? StoreImageUrl { get; init; }
        public IReadOnlyList<CategorySummaryResponse> Categories { get; init; } = [];
    }
}
