using System;

namespace Graduation_Project_Backend.Models.Entities
{

    public class RedeemCouponDto
    {
        public Guid UserId { get; set; }
        public long CouponId { get; set; }
    }

}

