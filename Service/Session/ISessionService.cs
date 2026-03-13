using Graduation_Project_Backend.Models.User;

namespace Graduation_Project_Backend.Service.Session
{
    public interface ISessionService
    {
        Task<UserSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<UserSession> CreateOrReplaceSessionAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    }
}
