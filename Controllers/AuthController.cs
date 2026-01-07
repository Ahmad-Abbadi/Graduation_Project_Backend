using Graduation_Project_Backend.DOTs;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private static readonly PasswordHasher<UserProfile> Hasher = new();
        private readonly ServiceClass _service;

        public AuthController(ServiceClass service)
        {
            _service = service;
        }

        [HttpPost("login-or-register")]
        public async Task<ActionResult<AuthResponseDto>> LoginOrRegister(LoginOrRegisterDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid body.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("PhoneNumber and Password are required.");

            var phone = NormalizePhone(dto.PhoneNumber);

            var user = await _service.GetUserByPhoneAsync(phone);

            if (user == null)
            {
                user = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = phone,
                    Name = dto.Name?.Trim() ?? "",
                    Role = "user",
                    TotalPoints = 0
                };

                user.PasswordHash = Hasher.HashPassword(user, dto.Password);

                await _service.CreateUserAsync(user);

                return Ok(ToResponse("Registered", user));
            }

            var verify = Hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid phone number or password.");

            return Ok(ToResponse("LoggedIn", user));
        }

        private static AuthResponseDto ToResponse(string msg, UserProfile user)
            => new()
            {
                Message = msg,
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                TotalPoints = user.TotalPoints,
                Role = user.Role
            };

        private static string NormalizePhone(string phone)
        {
            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            if (phone.StartsWith("07") && phone.Length == 10)
                return "+962" + phone[1..];

            return phone;
        }
    }
}
