using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project_Backend.Models.Entities
{
    [Table("offers")]
    public class Offer
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("store_id")]
        public Guid StoreId { get; set; }

        [Column("title")]
        public string Title { get; set; } = "";

        [Column("description")]
        public string? Description { get; set; }

        [Column("offer_type")]
        public string OfferType { get; set; } = "";

        [Column("discount_percent")]
        public decimal? DiscountPercent { get; set; }

        [Column("bonus_points")]
        public int? BonusPoints { get; set; }

        [Column("points_multiplier")]
        public decimal? PointsMultiplier { get; set; }

        [Column("start_at")]
        public DateTimeOffset StartAt { get; set; }

        [Column("end_at")]
        public DateTimeOffset EndAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        public Store? Store { get; set; }
    }
}
