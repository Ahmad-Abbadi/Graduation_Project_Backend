namespace Graduation_Project_Backend.DTOs.Announcements
{
    public sealed class ManageAnnouncementListItemResponse
    {
        public Guid Id { get; init; }
        public Guid? StoreId { get; init; }
        public string? StoreName { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool IsPinned { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
    }
}
