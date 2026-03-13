namespace Graduation_Project_Backend.Service.Auth
{
    public abstract class AuthException : Exception
    {
        protected AuthException(string message, string code)
            : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }

    public sealed class AuthValidationException : AuthException
    {
        public AuthValidationException(string message, string code)
            : base(message, code)
        {
        }
    }

    public sealed class AuthUnauthorizedException : AuthException
    {
        public AuthUnauthorizedException(string message, string code)
            : base(message, code)
        {
        }
    }

    public sealed class AuthConflictException : AuthException
    {
        public AuthConflictException(string message, string code)
            : base(message, code)
        {
        }
    }

    public sealed class AuthNotFoundException : AuthException
    {
        public AuthNotFoundException(string message, string code)
            : base(message, code)
        {
        }
    }
}
