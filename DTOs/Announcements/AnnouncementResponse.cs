namespace Graduation_Project_Backend.DTOs.Announcements
{
    public sealed class AnnouncementResponse
    {
        public Guid Id { get; init; }
        public Guid MallID { get; init; }
        public Guid? StoreId { get; init; }
        public string? StoreName { get; init; }
        public Guid ManagerId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string AnnouncementType { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool IsPinned { get; init; }
        public string? ImageUrl { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
