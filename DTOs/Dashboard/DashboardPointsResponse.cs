namespace Graduation_Project_Backend.DTOs.Dashboard
{
    public sealed class DashboardPointsResponse
    {
        public int TotalPointsIssued { get; init; }
        public decimal? TotalPointsRedeemed { get; init; }
        public IReadOnlyList<DailyIssuedPointsResponse> DailyIssued { get; init; } = [];
        public IReadOnlyList<DailyRedeemedPointsResponse> DailyRedeemed { get; init; } = [];
    }

    public sealed class DailyIssuedPointsResponse
    {
        public DateTime Date { get; init; }
        public int PointsIssued { get; init; }
    }

    public sealed class DailyRedeemedPointsResponse
    {
        public DateTime Date { get; init; }
        public decimal PointsRedeemed { get; init; }
    }
}
