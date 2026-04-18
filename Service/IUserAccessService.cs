namespace Graduation_Project_Backend.Service
{
    public interface IUserAccessService
    {
        Task<UserAccessContext> GetUserAccessContextAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
