using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Stores;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class StoresService : IStoresService
    {
        private readonly AppDbContext _db;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<StoresService> _logger;

        public StoresService(AppDbContext db, IUserAccessService userAccessService, ILogger<StoresService> logger)
        {
            _db = db;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<List<Store>> GetStoresAsync()
        {
            return await _db.Stores
                .AsNoTracking()
                .OrderBy(store => store.Name)
                .ToListAsync();
        }

        public async Task<Store?> GetStoreByIdAsync(Guid storeId)
        {
            return await _db.Stores
                .AsNoTracking()
                .SingleOrDefaultAsync(store => store.Id == storeId);
        }

        public async Task<IReadOnlyList<StoreResponse>> GetVisibleStoresAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            return await GetStoreResponsesAsync(
                _db.Stores.AsNoTracking().Where(store => store.MallID == access.MallID).OrderBy(store => store.Name),
                cancellationToken);
        }

        public async Task<StoreResponse?> GetVisibleStoreByIdAsync(Guid currentUserId, Guid storeId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            return await GetStoreResponseAsync(
                _db.Stores.AsNoTracking().Where(store => store.Id == storeId && store.MallID == access.MallID),
                cancellationToken);
        }

        public async Task<IReadOnlyList<ManageStoreListItemResponse>> GetManagedStoresAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetMallWideManagerAccessAsync(currentUserId, cancellationToken);

            List<Store> stores = await _db.Stores
                .AsNoTracking()
                .Where(store => store.MallID == access.MallID)
                .OrderBy(store => store.Name)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, List<string>> categoryNames = await GetCategoryNamesByStoreIdAsync(
                stores.Select(store => store.Id).ToList(),
                cancellationToken);

            return stores.Select(store => new ManageStoreListItemResponse
            {
                Id = store.Id,
                Name = store.Name,
                FloorNumber = store.FloorNumber,
                PhoneNumber = store.PhoneNumber,
                Email = store.Email,
                Categories = categoryNames.GetValueOrDefault(store.Id, [])
            }).ToList();
        }

        public async Task<StoreResponse> CreateStoreAsync(Guid currentUserId, CreateStoreRequest request, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetMallWideManagerAccessAsync(currentUserId, cancellationToken);
            string name = NormalizeRequired(request.Name, "Store name is required.");
            List<long> categoryIds = NormalizeCategoryIds(request.CategoryIds);

            await ValidateCategoryIdsAsync(access.MallID, categoryIds, cancellationToken);

            var store = new Store
            {
                Id = Guid.NewGuid(),
                MallID = access.MallID,
                Name = name,
                OperatingHours = NormalizeOptional(request.OperatingHours),
                SocialMediaLinks = JsonDocumentMapper.ToJsonDocument(request.SocialMediaLinks),
                Description = NormalizeOptional(request.Description),
                PhoneNumber = NormalizeOptional(request.PhoneNumber),
                Email = NormalizeOptional(request.Email),
                FloorNumber = NormalizeOptional(request.FloorNumber),
                StoreImageUrl = NormalizeOptional(request.StoreImageUrl)
            };

            _db.Stores.Add(store);
            await SyncStoreCategoriesAsync(store.Id, categoryIds, replaceExisting: false, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Manager {UserId} created store {StoreId} in mall {MallId}.", currentUserId, store.Id, store.MallID);

            return await GetStoreResponseByIdRequiredAsync(store.Id, cancellationToken);
        }

        public async Task<StoreResponse> UpdateStoreAsync(Guid currentUserId, Guid storeId, UpdateStoreRequest request, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetMallWideManagerAccessAsync(currentUserId, cancellationToken);
            Store store = await _db.Stores.SingleOrDefaultAsync(
                existingStore => existingStore.Id == storeId && existingStore.MallID == access.MallID,
                cancellationToken)
                ?? throw new ApiNotFoundException("Store not found.", "STORE_NOT_FOUND");

            string name = NormalizeRequired(request.Name, "Store name is required.");
            List<long> categoryIds = NormalizeCategoryIds(request.CategoryIds);

            await ValidateCategoryIdsAsync(access.MallID, categoryIds, cancellationToken);

            store.Name = name;
            store.OperatingHours = NormalizeOptional(request.OperatingHours);
            store.SocialMediaLinks = JsonDocumentMapper.ToJsonDocument(request.SocialMediaLinks);
            store.Description = NormalizeOptional(request.Description);
            store.PhoneNumber = NormalizeOptional(request.PhoneNumber);
            store.Email = NormalizeOptional(request.Email);
            store.FloorNumber = NormalizeOptional(request.FloorNumber);
            store.StoreImageUrl = NormalizeOptional(request.StoreImageUrl);

            await SyncStoreCategoriesAsync(store.Id, categoryIds, replaceExisting: true, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Manager {UserId} updated store {StoreId}.", currentUserId, storeId);

            return await GetStoreResponseByIdRequiredAsync(store.Id, cancellationToken);
        }

        private async Task<UserAccessContext> GetMallWideManagerAccessAsync(Guid currentUserId, CancellationToken cancellationToken)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            if (!access.IsManager)
                throw new ApiForbiddenException("Only managers can perform this action.", "MANAGER_REQUIRED");

            if (!access.IsMallWideManager)
                throw new ApiForbiddenException("Only mall-wide managers can manage stores.", "MALL_MANAGER_REQUIRED");

            return access;
        }

        private async Task<IReadOnlyList<StoreResponse>> GetStoreResponsesAsync(IQueryable<Store> query, CancellationToken cancellationToken)
        {
            List<Store> stores = await query.ToListAsync(cancellationToken);
            Dictionary<Guid, List<CategorySummaryResponse>> categories = await GetCategoriesByStoreIdAsync(
                stores.Select(store => store.Id).ToList(),
                cancellationToken);

            return stores.Select(store => MapStore(store, categories.GetValueOrDefault(store.Id, []))).ToList();
        }

        private async Task<StoreResponse?> GetStoreResponseAsync(IQueryable<Store> query, CancellationToken cancellationToken)
        {
            Store? store = await query.SingleOrDefaultAsync(cancellationToken);
            if (store == null)
                return null;

            Dictionary<Guid, List<CategorySummaryResponse>> categories = await GetCategoriesByStoreIdAsync([store.Id], cancellationToken);
            return MapStore(store, categories.GetValueOrDefault(store.Id, []));
        }

        private async Task<StoreResponse> GetStoreResponseByIdRequiredAsync(Guid storeId, CancellationToken cancellationToken)
            => await GetStoreResponseAsync(_db.Stores.AsNoTracking().Where(store => store.Id == storeId), cancellationToken)
                ?? throw new ApiNotFoundException("Store not found.", "STORE_NOT_FOUND");

        private async Task<Dictionary<Guid, List<CategorySummaryResponse>>> GetCategoriesByStoreIdAsync(
            IReadOnlyCollection<Guid> storeIds,
            CancellationToken cancellationToken)
        {
            if (storeIds.Count == 0)
                return [];

            var categoryRows = await (
                from storeCategory in _db.StoreCategories.AsNoTracking()
                join category in _db.Categories.AsNoTracking() on storeCategory.CategoryId equals category.Id
                where storeIds.Contains(storeCategory.StoreId)
                orderby category.Name
                select new
                {
                    storeCategory.StoreId,
                    Category = new CategorySummaryResponse
                    {
                        Id = category.Id,
                        Name = category.Name
                    }
                }).ToListAsync(cancellationToken);

            return categoryRows
                .GroupBy(row => row.StoreId)
                .ToDictionary(group => group.Key, group => group.Select(row => row.Category).ToList());
        }

        private async Task<Dictionary<Guid, List<string>>> GetCategoryNamesByStoreIdAsync(
            IReadOnlyCollection<Guid> storeIds,
            CancellationToken cancellationToken)
        {
            Dictionary<Guid, List<CategorySummaryResponse>> categories = await GetCategoriesByStoreIdAsync(storeIds, cancellationToken);
            return categories.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(category => category.Name).ToList());
        }

        private async Task ValidateCategoryIdsAsync(Guid mallId, IReadOnlyCollection<long> categoryIds, CancellationToken cancellationToken)
        {
            if (categoryIds.Count == 0)
                return;

            int validCount = await _db.Categories
                .AsNoTracking()
                .Where(category => category.MallID == mallId && categoryIds.Contains(category.Id))
                .CountAsync(cancellationToken);

            if (validCount != categoryIds.Count)
                throw new ApiValidationException("One or more category IDs are invalid for this mall.", "INVALID_CATEGORY_IDS");
        }

        private async Task SyncStoreCategoriesAsync(
            Guid storeId,
            IReadOnlyCollection<long> categoryIds,
            bool replaceExisting,
            CancellationToken cancellationToken)
        {
            if (replaceExisting)
            {
                List<StoreCategory> existingCategories = await _db.StoreCategories
                    .Where(storeCategory => storeCategory.StoreId == storeId)
                    .ToListAsync(cancellationToken);

                if (existingCategories.Count > 0)
                    _db.StoreCategories.RemoveRange(existingCategories);
            }

            if (categoryIds.Count == 0)
                return;

            List<StoreCategory> storeCategories = categoryIds
                .Select(categoryId => new StoreCategory
                {
                    StoreId = storeId,
                    CategoryId = categoryId
                })
                .ToList();

            _db.StoreCategories.AddRange(storeCategories);
        }

        private static StoreResponse MapStore(Store store, IReadOnlyList<CategorySummaryResponse> categories)
            => new()
            {
                Id = store.Id,
                Name = store.Name,
                MallID = store.MallID,
                OperatingHours = store.OperatingHours,
                SocialMediaLinks = JsonDocumentMapper.ToJsonElement(store.SocialMediaLinks),
                Description = store.Description,
                PhoneNumber = store.PhoneNumber,
                Email = store.Email,
                FloorNumber = store.FloorNumber,
                StoreImageUrl = store.StoreImageUrl,
                Categories = categories
            };

        private static string NormalizeRequired(string? value, string message)
        {
            string normalized = NormalizeOptional(value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ApiValidationException(message, "VALUE_REQUIRED");

            return normalized;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static List<long> NormalizeCategoryIds(IEnumerable<long>? categoryIds)
            => categoryIds?
                .Where(categoryId => categoryId > 0)
                .Distinct()
                .ToList()
                ?? [];
    }
}
