namespace Graduation_Project_Backend.Service
{
    public interface INotificationService
    {
        Task SendToAllMallUsersAsync(
            Guid mallId,
            string title,
            string message,
            string notificationType,
            CancellationToken cancellationToken = default);
    }
}
