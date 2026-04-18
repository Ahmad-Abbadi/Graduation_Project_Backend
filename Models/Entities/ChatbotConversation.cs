namespace Graduation_Project_Backend.Models.Entities
{
    public sealed class ChatbotConversation
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
        public Guid? MatchedFaqId { get; set; }
        public int? ResponseTimeMs { get; set; }
        public bool? WasHelpful { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
