using Graduation_Project_Backend.Data;
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
    }
}
