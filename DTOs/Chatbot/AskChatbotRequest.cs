using System.ComponentModel.DataAnnotations;

namespace Graduation_Project_Backend.DTOs.Chatbot
{
    public sealed class AskChatbotRequest
    {
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public Guid? ConversationSessionId { get; set; }
    }
}
