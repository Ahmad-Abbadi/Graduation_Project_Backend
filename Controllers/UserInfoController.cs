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
        private readonly IRewardsService _rewardsService;

        public UserInfoController(IRewardsService rewardsService)
        {
            _rewardsService = rewardsService;
        }

        [HttpGet("points")]
        public async Task<IActionResult> GetUserPoints()
        {
            var session = HttpContext.GetCurrentUserSession();
            var totalPoints = await _rewardsService.GetUserTotalPointsAsync(session.UserId);

            if (totalPoints == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                totalPoints = totalPoints.Value
            });
        }
    }
}
