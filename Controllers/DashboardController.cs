using Graduation_Project_Backend.DTOs.Dashboard;
using Graduation_Project_Backend.Extensions;
using Graduation_Project_Backend.Filters;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SessionRequired]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DashboardDateRangeQuery query, CancellationToken cancellationToken)
            => await ExecuteAsync(currentUserId => _dashboardService.GetSummaryAsync(currentUserId, query, cancellationToken));

        [HttpGet("sales")]
        public async Task<IActionResult> GetSales([FromQuery] DashboardDateRangeQuery query, CancellationToken cancellationToken)
            => await ExecuteAsync(currentUserId => _dashboardService.GetSalesAsync(currentUserId, query, cancellationToken));

        [HttpGet("points")]
        public async Task<IActionResult> GetPoints([FromQuery] DashboardDateRangeQuery query, CancellationToken cancellationToken)
            => await ExecuteAsync(currentUserId => _dashboardService.GetPointsAsync(currentUserId, query, cancellationToken));

        [HttpGet("coupons")]
        public async Task<IActionResult> GetCoupons([FromQuery] DashboardDateRangeQuery query, CancellationToken cancellationToken)
            => await ExecuteAsync(currentUserId => _dashboardService.GetCouponsAsync(currentUserId, query, cancellationToken));

        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity([FromQuery] DashboardDateRangeQuery query, CancellationToken cancellationToken)
            => await ExecuteAsync(currentUserId => _dashboardService.GetActivityAsync(currentUserId, query, cancellationToken));

        private async Task<IActionResult> ExecuteAsync<T>(Func<Guid, Task<T>> action)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                T response = await action(session.UserId);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new
                {
                    success = false,
                    error = new
                    {
                        code = ex.Code,
                        message = ex.Message
                    }
                });
            }
        }
    }
}
