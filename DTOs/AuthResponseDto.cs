namespace Graduation_Project_Backend.DTOs
{
    public class AuthResponseDto
    {
        public string Message { get; set; } = "";     
        public Guid UserId { get; set; }            
        public string PhoneNumber { get; set; } = "";
        public string Name { get; set; } = "";       
        public int TotalPoints { get; set; }         
        public string Role { get; set; } = "";       
        public string SessionId { get; set; } = "";  
    }
}
