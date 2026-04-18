using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.DTOs.Auth;
using Graduation_Project_Backend.Service.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto? dto, CancellationToken cancellationToken)
        {
            try
            {
                AuthResponseDto response = await _authService.RegisterAsync(dto, cancellationToken);
                return Ok(response);
            }
            catch (AuthValidationException ex)
            {
                return BadRequest(ToError(ex));
            }
            catch (AuthConflictException ex)
            {
                return Conflict(ToError(ex));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto? dto, CancellationToken cancellationToken)
        {
            try
            {
                AuthResponseDto response = await _authService.LoginAsync(dto, cancellationToken);
                return Ok(response);
            }
            catch (AuthValidationException ex)
            {
                return BadRequest(ToError(ex));
            }
            catch (AuthUnauthorizedException ex)
            {
                return Unauthorized(ToError(ex));
            }
        }

        [HttpPost("manager-quick-login")]
        public async Task<ActionResult<AuthResponseDto>> ManagerQuickLogin([FromBody] ManagerQuickLoginRequestDto? dto, CancellationToken cancellationToken)
        {
            try
            {
                AuthResponseDto response = await _authService.ManagerQuickLoginAsync(dto, cancellationToken);
                return Ok(response);
            }
            catch (AuthValidationException ex)
            {
                return BadRequest(ToError(ex));
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto? dto, CancellationToken cancellationToken)
        {
            try
            {
                await _authService.LogoutAsync(dto?.SessionId ?? string.Empty, cancellationToken);
                return Ok(new { message = "Logged out successfully." });
            }
            catch (AuthValidationException ex)
            {
                return BadRequest(ToError(ex));
            }
            catch (AuthNotFoundException ex)
            {
                return NotFound(ToError(ex));
            }
        }

        private static object ToError(AuthException exception)
            => new
            {
                success = false,
                error = new
                {
                    code = exception.Code,
                    message = exception.Message
                }
            };
    }
}
