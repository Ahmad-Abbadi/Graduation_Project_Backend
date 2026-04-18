using Graduation_Project_Backend.DTOs.Announcements;

namespace Graduation_Project_Backend.Service
{
    public interface IAnnouncementsService
    {
        Task<IReadOnlyList<AnnouncementResponse>> GetVisibleAnnouncementsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ManageAnnouncementListItemResponse>> GetManagedAnnouncementsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<AnnouncementResponse> CreateAnnouncementAsync(Guid currentUserId, CreateAnnouncementRequest request, CancellationToken cancellationToken = default);
        Task<AnnouncementResponse> UpdateAnnouncementAsync(Guid currentUserId, Guid announcementId, UpdateAnnouncementRequest request, CancellationToken cancellationToken = default);
        Task DeleteAnnouncementAsync(Guid currentUserId, Guid announcementId, CancellationToken cancellationToken = default);
        Task<AnnouncementResponse> SetAnnouncementStatusAsync(Guid currentUserId, Guid announcementId, bool isActive, CancellationToken cancellationToken = default);
        Task<AnnouncementResponse> SetAnnouncementPinAsync(Guid currentUserId, Guid announcementId, bool isPinned, CancellationToken cancellationToken = default);
    }
}
