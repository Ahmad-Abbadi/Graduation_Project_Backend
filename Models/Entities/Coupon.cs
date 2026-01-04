namespace Graduation_Project_Backend.Models.Entities
{
    public class Coupon
    {
        public long Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid ManagerId { get; set; }
        public string Type { get; set; } = "";
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? Bonus { get; set; }
        public bool IsActive { get; set; }
        public decimal? CostPoint { get; set; }
    }
}
