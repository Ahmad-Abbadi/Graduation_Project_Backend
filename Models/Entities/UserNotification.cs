namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class UserNotification
    {
        public Guid NotificationsId { get; set; }
        public Guid UserId { get; set; }
        public bool IsRead { get; set; }

        public Notification? Notification { get; set; }
    }
}
