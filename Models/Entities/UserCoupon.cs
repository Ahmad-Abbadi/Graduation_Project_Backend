using System;
using Graduation_Project_Backend.Models.User;

namespace Graduation_Project_Backend.Models.Entities
{
    public class UserCoupon
    {
        public string SerialNumber { get; set; } = null!;
        public Guid UserId { get; set; }
        public long CouponId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // optional navigation props
        public UserProfile? User { get; set; }
        public Coupon? Coupon { get; set; }
    }
}
