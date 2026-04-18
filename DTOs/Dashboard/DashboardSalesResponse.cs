namespace Graduation_Project_Backend.DTOs.Dashboard
{
    public sealed class DashboardSalesResponse
    {
        public decimal TotalSalesAmount { get; init; }
        public int TotalTransactions { get; init; }
        public IReadOnlyList<DailySalesPointResponse> DailySales { get; init; } = [];
        public IReadOnlyList<StoreSalesPointResponse> TopStores { get; init; } = [];
    }

    public sealed class DailySalesPointResponse
    {
        public DateTime Date { get; init; }
        public decimal SalesAmount { get; init; }
        public int TransactionsCount { get; init; }
    }

    public sealed class StoreSalesPointResponse
    {
        public Guid StoreId { get; init; }
        public string StoreName { get; init; } = string.Empty;
        public decimal SalesAmount { get; init; }
        public int TransactionsCount { get; init; }
    }
}
