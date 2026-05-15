using System.ComponentModel.DataAnnotations;

namespace Graduation_Project_Backend.DTOs.Coupons
{
    public sealed class CreateCouponRequest
    {
        [Required]
        [MaxLength(200)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? CostPoint { get; set; }
    }
}
