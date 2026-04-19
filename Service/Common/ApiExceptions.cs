using Microsoft.AspNetCore.Http;

namespace Graduation_Project_Backend.Service.Common
{
    public abstract class ApiException : Exception
    {
        protected ApiException(string message, string code, int statusCode)
            : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }

        public string Code { get; }
        public int StatusCode { get; }
    }

    public sealed class ApiValidationException : ApiException
    {
        public ApiValidationException(string message, string code = "VALIDATION_ERROR")
            : base(message, code, StatusCodes.Status400BadRequest)
        {
        }
    }

    public sealed class ApiUnauthorizedException : ApiException
    {
        public ApiUnauthorizedException(string message, string code = "UNAUTHORIZED")
            : base(message, code, StatusCodes.Status401Unauthorized)
        {
        }
    }

    public sealed class ApiForbiddenException : ApiException
    {
        public ApiForbiddenException(string message, string code = "FORBIDDEN")
            : base(message, code, StatusCodes.Status403Forbidden)
        {
        }
    }

    public sealed class ApiNotFoundException : ApiException
    {
        public ApiNotFoundException(string message, string code = "NOT_FOUND")
            : base(message, code, StatusCodes.Status404NotFound)
        {
        }
    }

    public sealed class ApiConflictException : ApiException
    {
        public ApiConflictException(string message, string code = "CONFLICT")
            : base(message, code, StatusCodes.Status409Conflict)
        {
        }
    }

    public sealed class ApiExternalServiceException : ApiException
    {
        public ApiExternalServiceException(string message, string code = "EXTERNAL_SERVICE_ERROR")
            : base(message, code, StatusCodes.Status502BadGateway)
        {
        }
    }
}
