using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserInfoController : ControllerBase
    {
        private readonly ServiceClass _service;

        public UserInfoController(ServiceClass service)
        {
            _service = service;
        }

        [HttpGet("points/{userId:guid}")]
        public async Task<IActionResult> GetUserPoints(Guid userId)
        {
            var totalPoints = await _service.GetUserTotalPointsAsync(userId);

            if (totalPoints == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                totalPoints = totalPoints.Value
            });
        }
    }
}