using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Announcements;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class AnnouncementsService : IAnnouncementsService
    {
        private readonly AppDbContext _db;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<AnnouncementsService> _logger;

        public AnnouncementsService(AppDbContext db, IUserAccessService userAccessService, ILogger<AnnouncementsService> logger)
        {
            _db = db;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<AnnouncementResponse>> GetVisibleAnnouncementsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return await BuildAnnouncementProjectionQuery(
                    _db.Announcements.AsNoTracking()
                        .Where(announcement =>
                            announcement.MallID == access.MallID &&
                            announcement.IsActive &&
                            announcement.StartDate <= now &&
                            announcement.EndDate >= now))
                .OrderByDescending(announcement => announcement.IsPinned)
                .ThenByDescending(announcement => announcement.Priority == "high")
                .ThenByDescending(announcement => announcement.StartDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ManageAnnouncementListItemResponse>> GetManagedAnnouncementsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);

            var items = await BuildAnnouncementProjectionQuery(FilterAnnouncementsByScope(access, _db.Announcements.AsNoTracking()))
                .OrderByDescending(announcement => announcement.UpdatedAt)
                .Select(announcement => new ManageAnnouncementListItemResponse
                {
                    Id = announcement.Id,
                    StoreId = announcement.StoreId,
                    StoreName = announcement.StoreName,
                    Title = announcement.Title,
                    Priority = announcement.Priority,
                    IsActive = announcement.IsActive,
                    IsPinned = announcement.IsPinned,
                    StartDate = announcement.StartDate,
                    EndDate = announcement.EndDate
                })
                .ToListAsync(cancellationToken);

            return items;
        }

        public async Task<AnnouncementResponse> CreateAnnouncementAsync(Guid currentUserId, CreateAnnouncementRequest request, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            await ValidateAnnouncementRequestAsync(access, request.StoreId, request.StartDate, request.EndDate, cancellationToken);

            var announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                MallID = access.MallID,
                StoreId = request.StoreId,
                ManagerId = currentUserId,
                Title = NormalizeRequired(request.Title, "Announcement title is required."),
                Content = NormalizeRequired(request.Content, "Announcement content is required."),
                AnnouncementType = NormalizeOptional(request.AnnouncementType) ?? "general",
                Priority = NormalizeOptional(request.Priority) ?? "normal",
                IsActive = request.IsActive,
                IsPinned = request.IsPinned,
                ImageUrl = NormalizeOptional(request.ImageUrl),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Announcements.Add(announcement);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Manager {UserId} created announcement {AnnouncementId}.", currentUserId, announcement.Id);

            return await GetAnnouncementByIdRequiredAsync(announcement.Id, cancellationToken);
        }

        public async Task<AnnouncementResponse> UpdateAnnouncementAsync(Guid currentUserId, Guid announcementId, UpdateAnnouncementRequest request, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Announcement announcement = await GetManagedAnnouncementEntityAsync(access, announcementId, cancellationToken);

            await ValidateAnnouncementRequestAsync(access, request.StoreId, request.StartDate, request.EndDate, cancellationToken);

            announcement.StoreId = request.StoreId;
            announcement.Title = NormalizeRequired(request.Title, "Announcement title is required.");
            announcement.Content = NormalizeRequired(request.Content, "Announcement content is required.");
            announcement.AnnouncementType = NormalizeOptional(request.AnnouncementType) ?? "general";
            announcement.Priority = NormalizeOptional(request.Priority) ?? "normal";
            announcement.IsActive = request.IsActive;
            announcement.IsPinned = request.IsPinned;
            announcement.ImageUrl = NormalizeOptional(request.ImageUrl);
            announcement.StartDate = request.StartDate;
            announcement.EndDate = request.EndDate;
            announcement.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Manager {UserId} updated announcement {AnnouncementId}.", currentUserId, announcementId);

            return await GetAnnouncementByIdRequiredAsync(announcement.Id, cancellationToken);
        }

        public async Task DeleteAnnouncementAsync(Guid currentUserId, Guid announcementId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Announcement announcement = await GetManagedAnnouncementEntityAsync(access, announcementId, cancellationToken);

            _db.Announcements.Remove(announcement);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<AnnouncementResponse> SetAnnouncementStatusAsync(Guid currentUserId, Guid announcementId, bool isActive, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Announcement announcement = await GetManagedAnnouncementEntityAsync(access, announcementId, cancellationToken);

            announcement.IsActive = isActive;
            announcement.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return await GetAnnouncementByIdRequiredAsync(announcement.Id, cancellationToken);
        }

        public async Task<AnnouncementResponse> SetAnnouncementPinAsync(Guid currentUserId, Guid announcementId, bool isPinned, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Announcement announcement = await GetManagedAnnouncementEntityAsync(access, announcementId, cancellationToken);

            announcement.IsPinned = isPinned;
            announcement.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return await GetAnnouncementByIdRequiredAsync(announcement.Id, cancellationToken);
        }

        private async Task<UserAccessContext> GetManagerAccessAsync(Guid currentUserId, CancellationToken cancellationToken)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            if (!access.IsManager)
                throw new ApiForbiddenException("Only managers can perform this action.", "MANAGER_REQUIRED");

            return access;
        }

        private IQueryable<Announcement> FilterAnnouncementsByScope(UserAccessContext access, IQueryable<Announcement> query)
            => access.IsMallWideManager
                ? query.Where(announcement => announcement.MallID == access.MallID)
                : query.Where(announcement => announcement.StoreId.HasValue && access.AssignedStoreIds.Contains(announcement.StoreId.Value));

        private async Task<Announcement> GetManagedAnnouncementEntityAsync(UserAccessContext access, Guid announcementId, CancellationToken cancellationToken)
            => await FilterAnnouncementsByScope(access, _db.Announcements)
                .SingleOrDefaultAsync(announcement => announcement.Id == announcementId, cancellationToken)
                ?? throw new ApiNotFoundException("Announcement not found.", "ANNOUNCEMENT_NOT_FOUND");

        private async Task ValidateAnnouncementRequestAsync(
            UserAccessContext access,
            Guid? storeId,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            if (startDate > endDate)
                throw new ApiValidationException("Start date must be earlier than end date.", "INVALID_DATE_RANGE");

            if (!storeId.HasValue)
            {
                if (!access.IsMallWideManager)
                    throw new ApiForbiddenException("Store managers can only create store-specific announcements.", "STORE_SCOPE_REQUIRED");

                return;
            }

            Store store = await _db.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(existingStore => existingStore.Id == storeId.Value, cancellationToken)
                ?? throw new ApiValidationException("Store does not exist.", "INVALID_STORE");

            if (store.MallID != access.MallID)
                throw new ApiForbiddenException("Store belongs to a different mall.", "STORE_OUTSIDE_SCOPE");

            if (!access.IsMallWideManager && !access.AssignedStoreIds.Contains(store.Id))
                throw new ApiForbiddenException("You are not allowed to manage announcements for this store.", "STORE_OUTSIDE_SCOPE");
        }

        private IQueryable<AnnouncementResponse> BuildAnnouncementProjectionQuery(IQueryable<Announcement> query)
            => from announcement in query
               join store in _db.Stores.AsNoTracking() on announcement.StoreId equals store.Id into stores
               from store in stores.DefaultIfEmpty()
               select new AnnouncementResponse
               {
                   Id = announcement.Id,
                   MallID = announcement.MallID,
                   StoreId = announcement.StoreId,
                   StoreName = store != null ? store.Name : null,
                   ManagerId = announcement.ManagerId,
                   Title = announcement.Title,
                   Content = announcement.Content,
                   AnnouncementType = announcement.AnnouncementType,
                   Priority = announcement.Priority,
                   IsActive = announcement.IsActive,
                   IsPinned = announcement.IsPinned,
                   ImageUrl = announcement.ImageUrl,
                   StartDate = announcement.StartDate,
                   EndDate = announcement.EndDate,
                   CreatedAt = announcement.CreatedAt,
                   UpdatedAt = announcement.UpdatedAt
               };

        private async Task<AnnouncementResponse> GetAnnouncementByIdRequiredAsync(Guid announcementId, CancellationToken cancellationToken)
            => await BuildAnnouncementProjectionQuery(_db.Announcements.AsNoTracking().Where(announcement => announcement.Id == announcementId))
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new ApiNotFoundException("Announcement not found.", "ANNOUNCEMENT_NOT_FOUND");

        private static string NormalizeRequired(string? value, string message)
        {
            string normalized = NormalizeOptional(value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ApiValidationException(message, "VALUE_REQUIRED");

            return normalized;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
