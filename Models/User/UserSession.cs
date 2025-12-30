namespace Graduation_Project_Backend.Models.User
{
    public class UserSession
    {
        public string Id { get; set; } = "";
        public Guid UserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public UserProfile User { get; set; } = null!;
    }
}
