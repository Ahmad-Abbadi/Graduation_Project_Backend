using System.Text.Json;

namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? NotificationType { get; set; }
        public long? CategoryId { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ScheduledFor { get; set; }
        public DateTimeOffset? SentAt { get; set; }
        public JsonDocument? Metadata { get; set; }
    }
}
