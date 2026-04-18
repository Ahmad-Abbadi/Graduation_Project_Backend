namespace Graduation_Project_Backend.DTOs.Dashboard
{
    public sealed class DashboardDateRangeQuery
    {
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }
}
