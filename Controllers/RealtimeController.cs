using System.Text.Json;
using Graduation_Project_Backend.DTOs.Realtime;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Realtime;
using Graduation_Project_Backend.Service.Session;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class RealtimeController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IUserPointsUpdatesService _userPointsUpdatesService;
        private readonly IRewardsService _rewardsService;

        public RealtimeController(
            ISessionService sessionService,
            IUserPointsUpdatesService userPointsUpdatesService,
            IRewardsService rewardsService)
        {
            _sessionService = sessionService;
            _userPointsUpdatesService = userPointsUpdatesService;
            _rewardsService = rewardsService;
        }

        [HttpGet("points-stream")]
        public async Task<IActionResult> StreamPoints([FromQuery] string? sessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Unauthorized(CreateError("SESSION_ID_REQUIRED", "Session id is required."));

            Models.User.UserSession? session = await _sessionService.GetSessionByIdAsync(sessionId, cancellationToken);
            if (session == null)
                return Unauthorized(CreateError("INVALID_SESSION", "Session id is invalid or expired."));

            int? totalPoints = await _rewardsService.GetUserTotalPointsAsync(session.UserId);
            if (totalPoints == null)
                return NotFound(CreateError("USER_NOT_FOUND", "User not found."));

            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers.Append("X-Accel-Buffering", "no");
            Response.ContentType = "text/event-stream";

            await Response.WriteAsync("retry: 5000\n\n", cancellationToken);
            await WriteEventAsync(new UserPointsChangedDto
            {
                UserId = session.UserId,
                TotalPoints = totalPoints.Value,
                Source = "initial",
                OccurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

            var updates = _userPointsUpdatesService.Subscribe(session.UserId, cancellationToken);
            await foreach (UserPointsChangedDto update in updates.ReadAllAsync(cancellationToken))
                await WriteEventAsync(update, cancellationToken);

            return new EmptyResult();
        }

        private async Task WriteEventAsync(UserPointsChangedDto payload, CancellationToken cancellationToken)
        {
            string json = JsonSerializer.Serialize(payload);
            await Response.WriteAsync("event: points-updated\n", cancellationToken);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        private static object CreateError(string code, string message)
            => new
            {
                success = false,
                error = new
                {
                    code,
                    message
                }
            };
    }
}
