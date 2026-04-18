using Graduation_Project_Backend.DTOs.Chatbot;

namespace Graduation_Project_Backend.Service
{
    public interface IChatbotService
    {
        Task<ChatbotAnswerResponse> AskAsync(Guid currentUserId, AskChatbotRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ChatbotHistoryItemResponse>> GetHistoryAsync(Guid currentUserId, Guid? conversationSessionId, CancellationToken cancellationToken = default);
    }
}
