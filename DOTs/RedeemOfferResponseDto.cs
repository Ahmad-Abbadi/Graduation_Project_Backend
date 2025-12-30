namespace Graduation_Project_Backend.DOTs
{
    public class RedeemOfferResponseDto
    {
        public long OfferId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public decimal FinalPrice { get; set; }
        public int EarnedPoints { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? PointsMultiplier { get; set; }
        public int PointsSpent { get; set; }
        public int RemainingPoints { get; set; }
        public string Message { get; set; } = "";
    }
}
