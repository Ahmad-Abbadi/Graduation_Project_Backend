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
    public sealed class NotificationsController : ControllerBase
    {
        private readonly INotificationsService _notificationsService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationsService notificationsService, ILogger<NotificationsController> logger)
        {
            _notificationsService = notificationsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var notifications = await _notificationsService.GetUserNotificationsAsync(session.UserId, unreadOnly, cancellationToken);
                return Ok(notifications);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var count = await _notificationsService.GetUnreadCountAsync(session.UserId, cancellationToken);
                return Ok(new { unreadCount = count });
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                await _notificationsService.MarkAsReadAsync(session.UserId, id, cancellationToken);
                return NoContent();
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                await _notificationsService.MarkAllAsReadAsync(session.UserId, cancellationToken);
                return NoContent();
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        private IActionResult ToErrorResult(ApiException exception)
            => StatusCode(exception.StatusCode, new
            {
                success = false,
                error = new { code = exception.Code, message = exception.Message }
            });
    }
}
