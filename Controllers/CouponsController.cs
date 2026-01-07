using Graduation_Project_Backend.DOTs;
using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly ServiceClass _service;

        public CouponsController(ServiceClass service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCoupons([FromQuery] bool? isActive)
        {
            var coupons = await _service.GetCouponsAsync(isActive);
            return Ok(coupons);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCouponById(Guid id)
        {
            var coupon = await _service.GetCouponDetailsAsync(id);
            if (coupon == null)
                return NotFound("Coupon not found.");

            return Ok(coupon);
        }

        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemCoupon(RedeemCouponDto dto)
        {
            try
            {
                var result = await _service.RedeemCouponAsync(dto.UserId, dto.CouponId);

                return Ok(new
                {
                    message = "Coupon redeemed successfully",
                    serial_number = result.SerialNumber
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserCoupons(Guid userId)
        {
            var coupons = await _service.GetUserCouponsViewAsync(userId);
            return Ok(coupons);
        }
    }
}
