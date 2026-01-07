namespace Graduation_Project_Backend.DOTs
{
    public class TransactionResultDto
    {
        public long TransactionId { get; set; }
        public Guid UserId { get; set; }
        public Guid StoreId { get; set; }
        public string ReceiptId { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Points { get; set; }
        public int NewTotalPoints { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}