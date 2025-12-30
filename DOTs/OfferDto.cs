namespace Graduation_Project_Backend.DOTs
{
    public class OfferDto
    {
        public long Id { get; set; }
        public Guid StoreId { get; set; }
        public string StoreName { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string OfferType { get; set; } = "";
        public decimal? DiscountPercent { get; set; }
        public int? BonusPoints { get; set; }
        public decimal? PointsMultiplier { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
