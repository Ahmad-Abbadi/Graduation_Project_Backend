namespace Graduation_Project_Backend.Models.User
{
    public class UserProfile
    {
        public Guid Id { get; set; }                
        public string Name { get; set; } = "";       
        public string PhoneNumber { get; set; } = ""; 
        public int TotalPoints { get; set; }        
        public string Role { get; set; } = "user";  
        public string PasswordHash { get; set; } = "";
    }
}
