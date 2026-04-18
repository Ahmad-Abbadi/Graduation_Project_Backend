namespace Graduation_Project_Backend.DTOs.Dashboard
{
    public sealed class DashboardActivityResponse
    {
        public int TotalOffers { get; init; }
        public int TotalAnnouncements { get; init; }
        public int ActiveOffers { get; init; }
        public int ActiveAnnouncements { get; init; }
        public int? UnreadNotifications { get; init; }
        public IReadOnlyList<RecentTransactionActivityResponse> RecentTransactions { get; init; } = [];
        public IReadOnlyList<CategoryDistributionResponse> CategoryDistribution { get; init; } = [];
    }

    public sealed class RecentTransactionActivityResponse
    {
        public long TransactionId { get; init; }
        public Guid StoreId { get; init; }
        public string StoreName { get; init; } = string.Empty;
        public string ReceiptId { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Points { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public string? Status { get; init; }
    }

    public sealed class CategoryDistributionResponse
    {
        public long CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public int StoresCount { get; init; }
    }
}
