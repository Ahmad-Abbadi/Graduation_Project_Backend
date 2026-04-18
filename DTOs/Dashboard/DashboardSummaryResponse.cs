namespace Graduation_Project_Backend.DTOs.Dashboard
{
    public sealed class DashboardSummaryResponse
    {
        public int TotalTransactions { get; init; }
        public decimal TotalSalesAmount { get; init; }
        public int TotalPointsIssued { get; init; }
        public decimal? TotalPointsRedeemed { get; init; }
        public int ActiveOffersCount { get; init; }
        public int ActiveAnnouncementsCount { get; init; }
        public int? RedeemedCouponsCount { get; init; }
        public int? ActivatedCouponsCount { get; init; }
    }
}
