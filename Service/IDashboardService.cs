using Graduation_Project_Backend.DTOs.Dashboard;

namespace Graduation_Project_Backend.Service
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetSummaryAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default);
        Task<DashboardSalesResponse> GetSalesAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default);
        Task<DashboardPointsResponse> GetPointsAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default);
        Task<DashboardCouponsResponse> GetCouponsAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default);
        Task<DashboardActivityResponse> GetActivityAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default);
    }
}
