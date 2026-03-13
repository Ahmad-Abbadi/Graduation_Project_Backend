using Graduation_Project_Backend.Service.Session;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Graduation_Project_Backend.Filters
{
    public sealed class SessionRequiredFilter : IAsyncActionFilter
    {
        private readonly ISessionService _sessionService;

        public SessionRequiredFilter(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string? sessionId = context.HttpContext.Request.Headers[SessionConstants.HeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                context.Result = new UnauthorizedObjectResult(CreateError("SESSION_ID_REQUIRED", "Session id is required."));
                return;
            }

            var session = await _sessionService.GetSessionByIdAsync(sessionId, context.HttpContext.RequestAborted);
            if (session == null)
            {
                context.Result = new UnauthorizedObjectResult(CreateError("INVALID_SESSION", "Session id is invalid or expired."));
                return;
            }

            context.HttpContext.Items[SessionConstants.HttpContextItemKey] = session;
            await next();
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
