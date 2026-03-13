using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CouponsController : ControllerBase
    {
        private readonly ServiceClass _service;

        public CouponsController(ServiceClass service)
        {
            _service = service;
        }

        [SessionRequired]
        [HttpGet]
        public async Task<IActionResult> GetCoupons([FromQuery] bool? isActive)
        {
            var coupons = await _service.GetCouponsAsync(isActive);
            return Ok(coupons);
        }

        [SessionRequired]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCouponById(Guid id)
        {
            var coupon = await _service.GetCouponDetailsAsync(id);
            if (coupon == null)
                return NotFound("Coupon not found.");

            return Ok(coupon);
        }

        [SessionRequired]
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemCoupon([FromBody] RedeemCouponDto? dto)
        {
            if (dto == null)
                return BadRequest("Request body is null.");

            if (dto.CouponId == Guid.Empty)
                return BadRequest("Coupon ID is required.");

            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var result = await _service.RedeemCouponAsync(session.UserId, dto.CouponId);

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

        [HttpPost("redeem-by-serial")]
        public async Task<IActionResult> RedeemCouponBySerial([FromBody] RedeemCouponBySerialDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.SerialNumber))
                return BadRequest("Serial number is required.");

            try
            {
                var result = await _service.RedeemCouponBySerialAsync(dto.SerialNumber);

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

        [SessionRequired]
        [HttpGet("user")]
        public async Task<IActionResult> GetUserCoupons()
        {
            var session = HttpContext.GetCurrentUserSession();
            var coupons = await _service.GetUserCouponsViewAsync(session.UserId);
            return Ok(coupons);
        }
    }
}
