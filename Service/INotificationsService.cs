using Graduation_Project_Backend.DTOs.Notifications;

namespace Graduation_Project_Backend.Service
{
    public interface INotificationsService
    {
        Task<IReadOnlyList<NotificationResponse>> GetUserNotificationsAsync(
            Guid userId,
            bool unreadOnly,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task MarkAsReadAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default);

        Task MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
