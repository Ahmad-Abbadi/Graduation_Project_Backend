namespace Graduation_Project_Backend.Models.User
{
    public sealed class UserProfile
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public string Role { get; set; } = "user";
        public string PasswordHash { get; set; } = string.Empty;
        public Guid MallID { get; set; }
    }
}
