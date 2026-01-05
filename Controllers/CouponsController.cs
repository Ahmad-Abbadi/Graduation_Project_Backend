using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CouponsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetCoupons([FromQuery] bool? isActive)
        {
            var query = _db.Coupons.AsNoTracking();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var coupons = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    created_at = c.CreatedAt,
                    manger_id = c.ManagerId,
                    type = c.Type,
                    start_at = c.StartAt,
                    end_at = c.EndAt,
                    discount_persent = c.DiscountPercent,
                    bouns = c.Bonus,
                    is_active = c.IsActive,
                    cost_point = c.CostPoint
                })
                .ToListAsync();

            return Ok(coupons);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetCouponById(long id)
        {
            var coupon = await _db.Coupons
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    created_at = c.CreatedAt,
                    manger_id = c.ManagerId,
                    type = c.Type,
                    start_at = c.StartAt,
                    end_at = c.EndAt,
                    discount_persent = c.DiscountPercent,
                    bouns = c.Bonus,
                    is_active = c.IsActive,
                    cost_point = c.CostPoint
                })
                .SingleOrDefaultAsync();

            if (coupon == null)
                return NotFound("Coupon not found.");

            return Ok(coupon);
        }


        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemCoupon([FromBody] RedeemCouponDto dto)
        {
            var coupon = await _db.Coupons
                .SingleOrDefaultAsync(c => c.Id == dto.CouponId);

            if (coupon == null)
                return NotFound("Coupon not found.");

            if (!coupon.IsActive)
                return BadRequest("Coupon is not active.");

            var now = DateTime.UtcNow;
            if (coupon.StartAt > now || coupon.EndAt < now)
                return BadRequest("Coupon is outside redeem period.");

            var user = await _db.UserProfiles
                .SingleOrDefaultAsync(u => u.Id == dto.UserId);

            if (user == null)
                return NotFound("User not found.");

            

            if (user.TotalPoints < coupon.CostPoint)
                return BadRequest("Not enough points.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Deduct points
                user.TotalPoints -= (int) coupon.CostPoint;
                _db.UserProfiles.Update(user);

                string serial;
                do
                {
                    serial = ServiceClass.Instance.GenerateSerialNumber();
                }
                while (await _db.UserCoupons.AnyAsync(x => x.SerialNumber == serial));
                // Create user coupon
                var userCoupon = new UserCoupon
                {
                    
                    SerialNumber = serial, //  STRING PK
                    UserId = dto.UserId,
                    CouponId = dto.CouponId
                };

                _db.UserCoupons.Add(userCoupon);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Coupon redeemed successfully",
                    serial_number = userCoupon.SerialNumber,
                    remaining_points = user.TotalPoints
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Failed to redeem coupon.");
            }
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserRedeemedCoupons(Guid userId)
        {
            var now = DateTimeOffset.UtcNow;

            var coupons = await _db.UserCoupons
                .AsNoTracking()
                .Where(uc => uc.UserId == userId)
                .Join(
                    _db.Coupons,
                    uc => uc.CouponId,
                    c => c.Id,
                    (uc, c) => new { uc, c }
                )
                .Where(x =>
                    x.c.IsActive &&
                    x.c.StartAt <= now &&
                    x.c.EndAt >= now
                )
                .Select(x => new
                {
                    serial_number = x.uc.SerialNumber,
                    type = x.c.Type,
                    discount_percent = x.c.DiscountPercent,
                    bonus = x.c.Bonus,
                    start_at = x.c.StartAt,
                    end_at = x.c.EndAt
                })
                .OrderBy(x => x.end_at)
                .ToListAsync();

            return Ok(coupons);
        }


    }
}
