namespace Graduation_Project_Backend.DTOs.Notifications
{
    public sealed class NotificationResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? NotificationType { get; init; }
        public bool IsRead { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? SentAt { get; init; }
    }
}
