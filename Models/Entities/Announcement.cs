namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Announcement
    {
        public Guid Id { get; set; }
        public Guid MallID { get; set; }
        public Guid? StoreId { get; set; }
        public Guid ManagerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AnnouncementType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPinned { get; set; }
        public string? ImageUrl { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
