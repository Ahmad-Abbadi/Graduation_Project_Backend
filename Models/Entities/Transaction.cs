using Graduation_Project_Backend.Models.User;

namespace Graduation_Project_Backend.Models.Entities  // ⬅️ Changed from Cahser_API.Models.Entities
{
    public class Transaction
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }
        public Guid StoreId { get; set; }
        public required string ReceiptId { get; set; }
        public string ReceiptDescription { get; set; }
        public required decimal Price { get; set; }
        public required int Points { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public UserProfile? User { get; set; }
    }
}