using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.DTOs.Auth;

namespace Graduation_Project_Backend.Service.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto? dto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto? dto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> ManagerQuickLoginAsync(ManagerQuickLoginRequestDto? dto, CancellationToken cancellationToken = default);
        Task LogoutAsync(string sessionId, CancellationToken cancellationToken = default);
    }
}
