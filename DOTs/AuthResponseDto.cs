namespace Graduation_Project_Backend.DOTs
{
    public class AuthResponseDto
    {
        public string Message { get; set; } = "";     // Registered / LoggedIn
        public Guid UserId { get; set; }            
        public string PhoneNumber { get; set; } = "";
        public string Name { get; set; } = "";       
        public int TotalPoints { get; set; }         
        public string Role { get; set; } = "";       
        public string SessionId { get; set; } = "";  
    }
}
