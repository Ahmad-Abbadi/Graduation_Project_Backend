using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service.Session
{
    public sealed class SessionService : ISessionService
    {
        private readonly AppDbContext _db;

        public SessionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<UserSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;

            string normalizedSessionId = sessionId.Trim();

            return await _db.UserSessions
                .Include(session => session.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(session => session.Id == normalizedSessionId, cancellationToken);
        }

        public async Task<UserSession> CreateOrReplaceSessionAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            List<UserSession> existingSessions = await _db.UserSessions
                .Where(session => session.UserId == userId)
                .ToListAsync(cancellationToken);

            if (existingSessions.Count > 0)
                _db.UserSessions.RemoveRange(existingSessions);

            var session = new UserSession
            {
                Id = GenerateSessionId(),
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.UserSessions.Add(session);
            return session;
        }

        public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            string normalizedSessionId = sessionId.Trim();
            UserSession? session = await _db.UserSessions
                .FirstOrDefaultAsync(existingSession => existingSession.Id == normalizedSessionId, cancellationToken);

            if (session == null)
                return false;

            _db.UserSessions.Remove(session);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string GenerateSessionId()
            => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
