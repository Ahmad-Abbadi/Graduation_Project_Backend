using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class UserAccessService : IUserAccessService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UserAccessService> _logger;

        public UserAccessService(AppDbContext db, ILogger<UserAccessService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<UserAccessContext> GetUserAccessContextAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            UserProfile user = await _db.UserProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(profile => profile.Id == userId, cancellationToken)
                ?? throw new ApiUnauthorizedException("Authenticated user was not found.", "USER_NOT_FOUND");

            Manager? manager = await _db.Managers
                .AsNoTracking()
                .SingleOrDefaultAsync(existingManager => existingManager.Id == userId, cancellationToken);

            HashSet<Guid> assignedStoreIds = [];

            if (manager != null)
            {
                List<Guid> assignedStoreIdList = await _db.Management
                    .AsNoTracking()
                    .Where(management => management.ManagerId == manager.Id)
                    .Select(management => management.StoreId)
                    .ToListAsync(cancellationToken);

                assignedStoreIds = assignedStoreIdList.ToHashSet();

                if (manager.MallID != user.MallID)
                {
                    _logger.LogWarning(
                        "Manager {ManagerId} has mall {ManagerMallId} but user profile is in mall {UserMallId}. Using manager mall for scope.",
                        manager.Id,
                        manager.MallID,
                        user.MallID);
                }
            }

            return new UserAccessContext
            {
                UserId = user.Id,
                MallID = manager?.MallID ?? user.MallID,
                UserRole = user.Role,
                IsManager = manager != null,
                IsMallWideManager = manager != null && assignedStoreIds.Count == 0,
                ManagerId = manager?.Id,
                ManagerRole = manager?.Role,
                AssignedStoreIds = assignedStoreIds
            };
        }
    }
}
