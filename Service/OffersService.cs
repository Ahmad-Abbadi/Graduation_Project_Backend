using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Offers;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class OffersService : IOffersService
    {
        private readonly AppDbContext _db;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<OffersService> _logger;

        public OffersService(AppDbContext db, IUserAccessService userAccessService, ILogger<OffersService> logger)
        {
            _db = db;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<List<Offer>> GetOffersAsync()
        {
            _logger.LogInformation("Loading offers from database.");

            List<Offer> offers = await _db.Offers
                .AsNoTracking()
                .OrderByDescending(offer => offer.MadeAt)
                .ToListAsync();

            _logger.LogInformation("Loaded {OfferCount} offers from database.", offers.Count);
            return offers;
        }

        public async Task<IReadOnlyList<OfferResponse>> GetVisibleOffersAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return await BuildOfferProjectionQuery(
                    _db.Offers.AsNoTracking()
                        .Where(offer =>
                            offer.MallID == access.MallID &&
                            offer.IsActive &&
                            offer.StartAt <= now &&
                            offer.EndAt >= now))
                .OrderByDescending(offer => offer.MadeAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ManageOfferListItemResponse>> GetManagedOffersAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);

            return await BuildOfferProjectionQuery(FilterOffersByScope(access, _db.Offers.AsNoTracking()))
                .OrderByDescending(offer => offer.MadeAt)
                .Select(offer => new ManageOfferListItemResponse
                {
                    Id = offer.Id,
                    StoreId = offer.StoreId,
                    StoreName = offer.StoreName,
                    Title = offer.Title,
                    IsActive = offer.IsActive,
                    StartAt = offer.StartAt,
                    EndAt = offer.EndAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<OfferResponse> CreateOfferAsync(Guid currentUserId, CreateOfferRequest request, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Store store = await GetScopedStoreAsync(access, request.StoreId, cancellationToken);
            ValidateDateRange(request.StartAt, request.EndAt);

            var offer = new Offer
            {
                StoreId = store.Id,
                MallID = store.MallID,
                Title = NormalizeRequired(request.Title, "Offer title is required."),
                Description = NormalizeOptional(request.Description),
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                IsActive = request.IsActive,
                MadeAt = DateTimeOffset.UtcNow
            };

            _db.Offers.Add(offer);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Manager {UserId} created offer {OfferId}.", currentUserId, offer.Id);

            return await GetOfferByIdRequiredAsync(offer.Id, cancellationToken);
        }

        public async Task<OfferResponse> UpdateOfferAsync(Guid currentUserId, long offerId, UpdateOfferRequest request, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Offer offer = await GetManagedOfferEntityAsync(access, offerId, cancellationToken);
            Store store = await GetScopedStoreAsync(access, request.StoreId, cancellationToken);
            ValidateDateRange(request.StartAt, request.EndAt);

            offer.StoreId = store.Id;
            offer.MallID = store.MallID;
            offer.Title = NormalizeRequired(request.Title, "Offer title is required.");
            offer.Description = NormalizeOptional(request.Description);
            offer.StartAt = request.StartAt;
            offer.EndAt = request.EndAt;
            offer.IsActive = request.IsActive;

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Manager {UserId} updated offer {OfferId}.", currentUserId, offerId);

            return await GetOfferByIdRequiredAsync(offer.Id, cancellationToken);
        }

        public async Task DeleteOfferAsync(Guid currentUserId, long offerId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Offer offer = await GetManagedOfferEntityAsync(access, offerId, cancellationToken);

            _db.Offers.Remove(offer);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<OfferResponse> SetOfferStatusAsync(Guid currentUserId, long offerId, bool isActive, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            Offer offer = await GetManagedOfferEntityAsync(access, offerId, cancellationToken);

            offer.IsActive = isActive;
            await _db.SaveChangesAsync(cancellationToken);

            return await GetOfferByIdRequiredAsync(offer.Id, cancellationToken);
        }

        private async Task<UserAccessContext> GetManagerAccessAsync(Guid currentUserId, CancellationToken cancellationToken)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            if (!access.IsManager)
                throw new ApiForbiddenException("Only managers can perform this action.", "MANAGER_REQUIRED");

            return access;
        }

        private IQueryable<Offer> FilterOffersByScope(UserAccessContext access, IQueryable<Offer> query)
            => access.IsMallWideManager
                ? query.Where(offer => offer.MallID == access.MallID)
                : query.Where(offer => access.AssignedStoreIds.Contains(offer.StoreId));

        private async Task<Offer> GetManagedOfferEntityAsync(UserAccessContext access, long offerId, CancellationToken cancellationToken)
            => await FilterOffersByScope(access, _db.Offers)
                .SingleOrDefaultAsync(offer => offer.Id == offerId, cancellationToken)
                ?? throw new ApiNotFoundException("Offer not found.", "OFFER_NOT_FOUND");

        private async Task<Store> GetScopedStoreAsync(UserAccessContext access, Guid storeId, CancellationToken cancellationToken)
        {
            Store store = await _db.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(existingStore => existingStore.Id == storeId, cancellationToken)
                ?? throw new ApiValidationException("Store does not exist.", "INVALID_STORE");

            if (store.MallID != access.MallID)
                throw new ApiForbiddenException("Store belongs to a different mall.", "STORE_OUTSIDE_SCOPE");

            if (!access.IsMallWideManager && !access.AssignedStoreIds.Contains(storeId))
                throw new ApiForbiddenException("You are not allowed to manage offers for this store.", "STORE_OUTSIDE_SCOPE");

            return store;
        }

        private IQueryable<OfferResponse> BuildOfferProjectionQuery(IQueryable<Offer> query)
            => from offer in query
               join store in _db.Stores.AsNoTracking() on offer.StoreId equals store.Id
               select new OfferResponse
               {
                   Id = offer.Id,
                   MallID = offer.MallID ?? store.MallID,
                   StoreId = offer.StoreId,
                   StoreName = store.Name,
                   Title = offer.Title,
                   Description = offer.Description,
                   StartAt = offer.StartAt,
                   EndAt = offer.EndAt,
                   IsActive = offer.IsActive,
                   MadeAt = offer.MadeAt
               };

        private async Task<OfferResponse> GetOfferByIdRequiredAsync(long offerId, CancellationToken cancellationToken)
            => await BuildOfferProjectionQuery(_db.Offers.AsNoTracking().Where(offer => offer.Id == offerId))
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new ApiNotFoundException("Offer not found.", "OFFER_NOT_FOUND");

        private static void ValidateDateRange(DateTimeOffset startAt, DateTimeOffset endAt)
        {
            if (startAt > endAt)
                throw new ApiValidationException("Start date must be earlier than end date.", "INVALID_DATE_RANGE");
        }

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
