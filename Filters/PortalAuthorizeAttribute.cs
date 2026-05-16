using Graduation_Project_Backend.Service.Portal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Graduation_Project_Backend.Filters
{
    public sealed class PortalAuthorizeAttribute : TypeFilterAttribute
    {
        public PortalAuthorizeAttribute(params string[] roles)
            : base(typeof(PortalAuthorizeFilter))
        {
            Arguments = [roles];
        }
    }

    public sealed class PortalAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _roles;
        private readonly Service.Session.ISessionService _sessionService;

        public PortalAuthorizeFilter(string[] roles, Service.Session.ISessionService sessionService)
        {
            _roles = roles;
            _sessionService = sessionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            string? sessionId = context.HttpContext.Request.Cookies[PortalConstants.SessionCookieName];
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                context.Result = new RedirectToActionResult("Login", "Portal", new { returnUrl = context.HttpContext.Request.Path.Value });
                return;
            }

            var session = await _sessionService.GetSessionByIdAsync(sessionId, context.HttpContext.RequestAborted);
            if (session?.User == null)
            {
                context.HttpContext.Response.Cookies.Delete(PortalConstants.SessionCookieName);
                context.Result = new RedirectToActionResult("Login", "Portal", new { returnUrl = context.HttpContext.Request.Path.Value });
                return;
            }

            if (_roles.Length > 0 && !_roles.Any(role => RoleMatches(session.User.Role, role)))
            {
                context.Result = new RedirectToActionResult("Denied", "Portal", null);
                return;
            }

            context.HttpContext.Items[Service.Session.SessionConstants.HttpContextItemKey] = session;
        }

        private static bool RoleMatches(string actualRole, string requiredRole)
        {
            if (string.Equals(requiredRole, PortalConstants.ManagerRole, StringComparison.OrdinalIgnoreCase))
                return actualRole.Contains(PortalConstants.ManagerRole, StringComparison.OrdinalIgnoreCase);

            return string.Equals(actualRole, requiredRole, StringComparison.OrdinalIgnoreCase);
        }
    }
}
