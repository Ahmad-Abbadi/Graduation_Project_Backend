namespace Graduation_Project_Backend.DTOs.Receipts
{
    public sealed class ReceiptListItemResponse
    {
        public long TransactionId { get; init; }
        public string ReceiptId { get; init; } = string.Empty;
        public Guid StoreId { get; init; }
        public string StoreName { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int PointsEarned { get; init; }
        public string? ReceiptDescription { get; init; }
        public string? ReceiptImageUrl { get; init; }
        public string? Status { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
