using Graduation_Project_Backend.DTOs.Chatbot;
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
    public sealed class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AskChatbotRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var session = HttpContext.GetCurrentUserSession();
                var response = await _chatbotService.AskAsync(session.UserId, request, cancellationToken);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] Guid? conversationSessionId, CancellationToken cancellationToken)
        {
            var session = HttpContext.GetCurrentUserSession();
            var history = await _chatbotService.GetHistoryAsync(session.UserId, conversationSessionId, cancellationToken);
            return Ok(history);
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
