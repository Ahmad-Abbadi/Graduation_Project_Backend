namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class Coupon
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid ManagerId { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public string Discription { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal? CostPoint { get; set; }
        public Guid MallID { get; set; }
    }
}
