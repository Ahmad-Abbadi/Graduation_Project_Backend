using Graduation_Project_Backend.DTOs;
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
            var phone = "";
            try
            {
                phone = _service.NormalizePhone(dto.PhoneNumber);

            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Invalid phone number: {ex.Message}");
                return BadRequest(new
                {
                    success = false,
                    error = new
                    {
                        code = "INVALID_PHONE_NUMBER",
                        message = ex.Message,
                        field = "phoneNumber"
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "INTERNAL_SERVER_ERROR",
                        message = "An unexpected error occurred while processing the phone number."
                    }
                });
            }

            var user = await _service.GetUserByPhoneAndMallIDAsync(phone, dto.MallID);

            if (user == null)
            {
                if ((string.IsNullOrWhiteSpace(dto.Name)))
                {
                    return Unauthorized("Invalid phone number or password.");
                }
                user = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = phone,
                    Name = dto.Name?.Trim() ?? "",
                    Role = "user",
                    TotalPoints = 0,
                    MallID=dto.MallID
                };

                user.PasswordHash = Hasher.HashPassword(user, dto.Password);

                await _service.CreateUserAsync(user);

                return Ok(ToResponse("Registered", user));
            }
            Console.WriteLine("user is loged in");
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
    }
}
