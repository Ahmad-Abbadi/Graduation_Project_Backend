using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Notifications;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class NotificationsService : INotificationsService
    {
        private readonly AppDbContext _db;

        public NotificationsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<NotificationResponse>> GetUserNotificationsAsync(
            Guid userId,
            bool unreadOnly,
            CancellationToken cancellationToken = default)
        {
            var query = from un in _db.UserNotifications.AsNoTracking()
                        join n in _db.Notifications.AsNoTracking() on un.NotificationsId equals n.Id
                        where un.UserId == userId
                        select new { un, n };

            if (unreadOnly)
                query = query.Where(x => !x.un.IsRead);

            return await query
                .OrderByDescending(x => x.n.CreatedAt)
                .Select(x => new NotificationResponse
                {
                    Id = x.n.Id,
                    Title = x.n.Title,
                    Message = x.n.Message,
                    NotificationType = x.n.NotificationType,
                    IsRead = x.un.IsRead,
                    CreatedAt = x.n.CreatedAt,
                    SentAt = x.n.SentAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _db.UserNotifications
                .AsNoTracking()
                .CountAsync(un => un.UserId == userId && !un.IsRead, cancellationToken);
        }

        public async Task MarkAsReadAsync(
            Guid userId,
            Guid notificationId,
            CancellationToken cancellationToken = default)
        {
            var link = await _db.UserNotifications
                .SingleOrDefaultAsync(un => un.NotificationsId == notificationId && un.UserId == userId, cancellationToken)
                ?? throw new ApiNotFoundException("Notification not found.", "NOTIFICATION_NOT_FOUND");

            if (!link.IsRead)
            {
                link.IsRead = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            await _db.UserNotifications
                .Where(un => un.UserId == userId && !un.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(un => un.IsRead, true), cancellationToken);
        }
    }
}
