using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Filters
{
    public sealed class SessionRequiredAttribute : TypeFilterAttribute
    {
        public SessionRequiredAttribute()
            : base(typeof(SessionRequiredFilter))
        {
        }
    }
}
