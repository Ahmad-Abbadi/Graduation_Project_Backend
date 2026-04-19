using Graduation_Project_Backend.DTOs.Chatbot;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                var response = await _chatbotService.AskAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ApiException ex)
            {
                return ToErrorResult(ex);
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
        {
            var history = await _chatbotService.GetHistoryAsync(cancellationToken);
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
