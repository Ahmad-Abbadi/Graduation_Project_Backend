namespace Cahser_API.Models
{
    public class AddTransactionDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid StoreId { get; set; }

        public required string ReceiptId { get; set; } = null!;

        public required double Price { get; set; }

        public required int Points { get; set; }

        public Guid? ValidatedBy { get; set; }

        public DateTimeOffset? ValidatedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }


    }
}
