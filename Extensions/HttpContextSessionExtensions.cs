using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Session;

namespace Graduation_Project_Backend.Extensions
{
    public static class HttpContextSessionExtensions
    {
        public static UserSession GetCurrentUserSession(this HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(SessionConstants.HttpContextItemKey, out object? sessionObject) &&
                sessionObject is UserSession session)
            {
                return session;
            }

            throw new InvalidOperationException("The current request does not have a resolved user session.");
        }
    }
}
