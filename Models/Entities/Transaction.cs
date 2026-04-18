using Graduation_Project_Backend.Models.User;

namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Transaction
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public Guid StoreId { get; set; }
        public required string ReceiptId { get; set; }
        public string ReceiptDescription { get; set; } = string.Empty;
        public required decimal Price { get; set; }
        public required int Points { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? ReceiptUrl { get; set; }
        public string? ReceiptImageUrl { get; set; }
        public string? TransactionStatus { get; set; }
        public UserProfile? User { get; set; }
        public Store? Store { get; set; }
    }
}
