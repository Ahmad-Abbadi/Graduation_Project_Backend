using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SendToAllMallUsersAsync(
            Guid mallId,
            string title,
            string message,
            string notificationType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[NOTIF] Sending — mall={MallId} type={Type}", mallId, notificationType);

            // Fetch regular users only (exclude managers)
            var userIds = await _db.UserProfiles
                .AsNoTracking()
                .Where(u => u.MallID == mallId && !_db.Managers.Any(m => m.Id == u.Id))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("[NOTIF] Found {Count} users for mall {MallId}", userIds.Count, mallId);

            if (userIds.Count == 0)
                return;

            var now = DateTimeOffset.UtcNow;

            // 1. Insert ONE shared notification row
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                NotificationType = notificationType,
                IsRead = false,
                CreatedAt = now,
                SentAt = now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync(cancellationToken);

            // 2. Insert one row per user in User_notifications
            var userNotifications = userIds.Select(userId => new UserNotification
            {
                NotificationsId = notification.Id,
                UserId = userId,
                IsRead = false
            }).ToList();

            _db.UserNotifications.AddRange(userNotifications);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[NOTIF] Done — 1 notification + {Count} user links saved.", userIds.Count);
        }
    }
}
