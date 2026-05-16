using Graduation_Project_Backend.Models.ViewModels;

namespace Graduation_Project_Backend.Service
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync(Guid currentUserId, CancellationToken cancellationToken = default);
        Task<PortalDashboardReportViewModel> GetDashboardReportAsync(Guid currentUserId, string? period, CancellationToken cancellationToken = default);
        Task<AdminMallsPageViewModel> GetMallsPageAsync(CancellationToken cancellationToken = default);
        Task<AdminStoresPageViewModel> GetStoresPageAsync(Guid? editStoreId = null, CancellationToken cancellationToken = default);
        Task<AdminManagersPageViewModel> GetManagersPageAsync(Guid? editManagerId = null, CancellationToken cancellationToken = default);
        Task<Guid> CreateMallAsync(AdminMallForm form, CancellationToken cancellationToken = default);
        Task<Guid> CreateStoreAsync(AdminStoreForm form, CancellationToken cancellationToken = default);
        Task UpdateStoreAsync(AdminStoreForm form, CancellationToken cancellationToken = default);
        Task<Guid> CreateManagerAsync(AdminManagerForm form, CancellationToken cancellationToken = default);
        Task UpdateManagerAsync(AdminManagerForm form, CancellationToken cancellationToken = default);
        Task DeleteManagerAsync(Guid managerId, CancellationToken cancellationToken = default);
    }
}
