namespace Graduation_Project_Backend.DTOs.Dashboard
{
    public sealed class DashboardCouponsResponse
    {
        public bool IsScopeLimited { get; init; }
        public int? TotalActiveCoupons { get; init; }
        public int? TotalActivatedUserCoupons { get; init; }
        public int? TotalRedeemedCoupons { get; init; }
        public decimal? RedemptionRate { get; init; }
    }
}
