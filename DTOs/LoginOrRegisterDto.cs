namespace Graduation_Project_Backend.DTOs
{
    public class LoginOrRegisterDto
    {
        public string PhoneNumber { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Name { get; set; }
        public Guid MallID { get; set; }
    }
}
