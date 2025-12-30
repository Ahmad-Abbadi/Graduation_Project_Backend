using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DOTs;
using Graduation_Project_Backend.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static readonly PasswordHasher<UserProfile> Hasher = new();
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("login-or-register")]
        public async Task<ActionResult<AuthResponseDto>> LoginOrRegister(LoginOrRegisterDto dto)
        {
            if (dto == null) return BadRequest("Invalid body.");
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("PhoneNumber and Password are required.");

            var phone = NormalizePhone(dto.PhoneNumber);

            var user = await _db.UserProfiles
                .SingleOrDefaultAsync(u => u.PhoneNumber == phone);

            if (user == null)
            {
                user = new UserProfile
                {
                    Id = Guid.NewGuid(),                 
                    PhoneNumber = phone,
                    Name = string.IsNullOrWhiteSpace(dto.Name) ? "" : dto.Name!.Trim(),
                    TotalPoints = 0,
                    Role = "user"
                };

                user.PasswordHash = Hasher.HashPassword(user, dto.Password);

                var sessionId = CreateSession();
                _db.UserProfiles.Add(user);
                _db.UserSessions.Add(new UserSession
                {
                    Id = sessionId,
                    UserId = user.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                return Ok(ToResponse("Registered", user, sessionId));
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return StatusCode(500, "User exists but password data is missing (dev storage issue).");

            var verify = Hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid phone number or password.");

            var session = CreateSession();
            _db.UserSessions.Add(new UserSession
            {
                Id = session,
                UserId = user.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return Ok(ToResponse("LoggedIn", user, session));
        }

        [HttpGet("me")]
        public async Task<ActionResult<object>> Me([FromHeader(Name = "X-Session-Id")] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Unauthorized("Missing X-Session-Id header.");

            var session = await _db.UserSessions
                .AsNoTracking()
                .Include(s => s.User)
                .SingleOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return Unauthorized("Invalid session.");

            return Ok(new
            {
                session.User.Id,
                session.User.Name,
                phone_number = session.User.PhoneNumber,
                total_points = session.User.TotalPoints,
                role = session.User.Role
            });
        }

        private static AuthResponseDto ToResponse(string msg, UserProfile user, string sessionId)
            => new AuthResponseDto
            {
                Message = msg,
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                TotalPoints = user.TotalPoints,
                Role = user.Role,
                SessionId = sessionId
            };

        private static string CreateSession()
        {
            var sessionId = Guid.NewGuid().ToString("N");
            return sessionId;
        }

        private static string NormalizePhone(string phone)
        {
            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            if (phone.StartsWith("07") && phone.Length == 10)
                return "+962" + phone.Substring(1);

            if (phone.StartsWith("+"))
                return phone;

            return phone;
        }
    }
}
