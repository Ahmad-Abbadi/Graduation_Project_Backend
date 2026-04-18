namespace Graduation_Project_Backend.DTOs.Stores
{
    public sealed class ManageStoreListItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? FloorNumber { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Email { get; init; }
        public IReadOnlyList<string> Categories { get; init; } = [];
    }
}
