namespace Graduation_Project_Backend.Service.Portal
{
    public interface IPortalAuthService
    {
        Task<PortalLoginResult> LoginAsync(PortalLoginRequest request, CancellationToken cancellationToken = default);
        Task<PortalLoginResult> RegisterAdminAsync(PortalRegisterAdminRequest request, CancellationToken cancellationToken = default);
        Task<PortalAccountRequest> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task UpdateAccountAsync(Guid userId, PortalAccountRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(string? sessionId, CancellationToken cancellationToken = default);
    }
}
