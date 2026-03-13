using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SessionRequired]
    public sealed class UserInfoController : ControllerBase
    {
        private readonly ServiceClass _service;

        public UserInfoController(ServiceClass service)
        {
            _service = service;
        }

        [HttpGet("points")]
        public async Task<IActionResult> GetUserPoints()
        {
            var session = HttpContext.GetCurrentUserSession();
            var totalPoints = await _service.GetUserTotalPointsAsync(session.UserId);

            if (totalPoints == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                totalPoints = totalPoints.Value
            });
        }
    }
}
