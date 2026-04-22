using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Graduation_Project_Backend.DTOs.Chatbot
{
    public sealed class AskChatbotRequest
    {
        [Required]
        [MaxLength(1000)]
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        public Guid? ConversationSessionId { get; set; }

        public string? GetMessage()
            => Message;
    }
}
