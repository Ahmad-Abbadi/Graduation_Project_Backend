using Graduation_Project_Backend.DTOs.Announcements;
using Graduation_Project_Backend.DTOs.Common;
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
    public sealed class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementsService _announcementsService;

        public AnnouncementsController(IAnnouncementsService announcementsService)
        {
            _announcementsService = announcementsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncements(CancellationToken cancellationToken)
        {
            var session = HttpContext.GetCurrentUserSession();
            var announcements = await _announcementsService.GetVisibleAnnouncementsAsync(session.UserId, cancellationToken);
            return Ok(announcements);
        }

        [HttpGet("manage")]
        public async Task<IActionResult> GetManagedAnnouncements(CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var announcements = await _announcementsService.GetManagedAnnouncementsAsync(session.UserId, cancellationToken);
                return Ok(announcements);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var announcement = await _announcementsService.CreateAnnouncementAsync(session.UserId, request, cancellationToken);
                return CreatedAtAction(nameof(GetAnnouncements), new { id = announcement.Id }, announcement);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var announcement = await _announcementsService.UpdateAnnouncementAsync(session.UserId, id, request, cancellationToken);
                return Ok(announcement);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                await _announcementsService.DeleteAnnouncementAsync(session.UserId, id, cancellationToken);
                return NoContent();
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> SetAnnouncementStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var announcement = await _announcementsService.SetAnnouncementStatusAsync(session.UserId, id, request.IsActive, cancellationToken);
                return Ok(announcement);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpPatch("{id:guid}/pin")]
        public async Task<IActionResult> SetAnnouncementPin(Guid id, [FromBody] SetAnnouncementPinRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var announcement = await _announcementsService.SetAnnouncementPinAsync(session.UserId, id, request.IsPinned, cancellationToken);
                return Ok(announcement);
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
                error = new
                {
                    code = exception.Code,
                    message = exception.Message
                }
            });
    }
}
