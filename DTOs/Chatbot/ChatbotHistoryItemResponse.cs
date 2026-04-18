namespace Graduation_Project_Backend.DTOs.Chatbot
{
    public sealed class ChatbotHistoryItemResponse
    {
        public Guid ConversationId { get; init; }
        public Guid ConversationSessionId { get; init; }
        public string UserMessage { get; init; } = string.Empty;
        public string BotResponse { get; init; } = string.Empty;
        public Guid? MatchedFaqId { get; init; }
        public int? ResponseTimeMs { get; init; }
        public bool? WasHelpful { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
