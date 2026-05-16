namespace Graduation_Project_Backend.Service.Portal
{
    public sealed class PortalLoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class PortalLoginResult
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public sealed class PortalRegisterAdminRequest
    {
        public string Name { get; set; } = "Admin";
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class PortalAccountRequest
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Password { get; set; }
    }
}
