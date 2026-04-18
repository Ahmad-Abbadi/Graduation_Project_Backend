namespace Graduation_Project_Backend.DTOs.Chatbot
{
    public sealed class ChatbotAnswerResponse
    {
        public Guid ConversationId { get; init; }
        public Guid ConversationSessionId { get; init; }
        public string UserMessage { get; init; } = string.Empty;
        public string BotResponse { get; init; } = string.Empty;
        public Guid? MatchedFaqId { get; init; }
        public string MatchSource { get; init; } = string.Empty;
        public int ResponseTimeMs { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
