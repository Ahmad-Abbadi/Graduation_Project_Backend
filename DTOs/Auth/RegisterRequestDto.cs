namespace Graduation_Project_Backend.DTOs.Auth
{
    public sealed class RegisterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid MallID { get; set; }
        public Guid? ManagerId { get; set; }
    }
}
