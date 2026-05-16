using Graduation_Project_Backend.Models.ViewModels;

namespace Graduation_Project_Backend.Service
{
    public interface IManagerPortalService
    {
        Task<ManagerDashboardViewModel> GetDashboardAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<PortalDashboardReportViewModel> GetDashboardReportAsync(Guid currentUserId, string? period, CancellationToken cancellationToken = default);
        Task<ManagerStoresPageViewModel> GetStoresPageAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<ManagerStoreManagersPageViewModel> GetStoreManagersPageAsync(Guid currentUserId, Guid? editManagerId = null, CancellationToken cancellationToken = default);
        Task<ManagerOffersPageViewModel> GetOffersPageAsync(Guid currentUserId, long? editOfferId = null, CancellationToken cancellationToken = default);
        Task<ManagerCouponsPageViewModel> GetCouponsPageAsync(Guid currentUserId, Guid? editCouponId = null, CancellationToken cancellationToken = default);
        Task<ManagerAnnouncementsPageViewModel> GetAnnouncementsPageAsync(Guid currentUserId, Guid? editAnnouncementId = null, CancellationToken cancellationToken = default);
        Task<Guid> CreateStoreAsync(Guid currentUserId, AdminStoreForm form, CancellationToken cancellationToken = default);
        Task<Guid> CreateStoreManagerAsync(Guid currentUserId, AdminManagerForm form, CancellationToken cancellationToken = default);
        Task UpdateStoreManagerAsync(Guid currentUserId, AdminManagerForm form, CancellationToken cancellationToken = default);
        Task DeleteStoreManagerAsync(Guid currentUserId, Guid managerId, CancellationToken cancellationToken = default);
        Task CreateOfferAsync(Guid currentUserId, ManagerOfferForm form, CancellationToken cancellationToken = default);
        Task UpdateOfferAsync(Guid currentUserId, ManagerOfferForm form, CancellationToken cancellationToken = default);
        Task DeleteOfferAsync(Guid currentUserId, long offerId, CancellationToken cancellationToken = default);
        Task CreateCouponAsync(Guid currentUserId, ManagerCouponForm form, CancellationToken cancellationToken = default);
        Task UpdateCouponAsync(Guid currentUserId, ManagerCouponForm form, CancellationToken cancellationToken = default);
        Task DeleteCouponAsync(Guid currentUserId, Guid couponId, CancellationToken cancellationToken = default);
        Task CreateAnnouncementAsync(Guid currentUserId, ManagerAnnouncementForm form, CancellationToken cancellationToken = default);
        Task UpdateAnnouncementAsync(Guid currentUserId, ManagerAnnouncementForm form, CancellationToken cancellationToken = default);
        Task DeleteAnnouncementAsync(Guid currentUserId, Guid announcementId, CancellationToken cancellationToken = default);
    }
}
