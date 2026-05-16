using System.Text.Json;
using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Dashboard;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Models.ViewModels;
using Graduation_Project_Backend.Service.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class AdminService : IAdminService
    {
        private readonly AppDbContext _db;
        private readonly IPhoneNumberService _phoneNumberService;
        private readonly IPasswordHasher<UserProfile> _passwordHasher;
        private readonly IDashboardService _dashboardService;

        public AdminService(
            AppDbContext db,
            IPhoneNumberService phoneNumberService,
            IPasswordHasher<UserProfile> passwordHasher,
            IDashboardService dashboardService)
        {
            _db = db;
            _phoneNumberService = phoneNumberService;
            _passwordHasher = passwordHasher;
            _dashboardService = dashboardService;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.Dashboard.DashboardDateRangeQuery();

            return new()
            {
                MallsCount = await _db.Malls.AsNoTracking().CountAsync(cancellationToken),
                StoresCount = await _db.Stores.AsNoTracking().CountAsync(cancellationToken),
                ManagersCount = await _db.Managers.AsNoTracking().CountAsync(cancellationToken),
                StoreAssignmentsCount = await _db.Management.AsNoTracking().CountAsync(cancellationToken),
                Summary = await _dashboardService.GetSummaryAsync(currentUserId, query, cancellationToken),
                Sales = await _dashboardService.GetSalesAsync(currentUserId, query, cancellationToken),
                Points = await _dashboardService.GetPointsAsync(currentUserId, query, cancellationToken),
                Coupons = await _dashboardService.GetCouponsAsync(currentUserId, query, cancellationToken),
                Activity = await _dashboardService.GetActivityAsync(currentUserId, query, cancellationToken)
            };
        }

        public async Task<PortalDashboardReportViewModel> GetDashboardReportAsync(Guid currentUserId, string? period, CancellationToken cancellationToken = default)
            => await BuildDashboardReportAsync(currentUserId, "All malls", period, canManageMallOperations: true, cancellationToken);

        public async Task<AdminMallsPageViewModel> GetMallsPageAsync(CancellationToken cancellationToken = default)
        {
            List<AdminMallListItem> malls = await (
                from mall in _db.Malls.AsNoTracking()
                orderby mall.CreatedAt descending, mall.Name
                select new AdminMallListItem
                {
                    Id = mall.Id,
                    Name = mall.Name,
                    CreatedAt = mall.CreatedAt,
                    StoresCount = _db.Stores.Count(store => store.MallID == mall.Id),
                    ManagersCount = _db.Managers.Count(manager => manager.MallID == mall.Id)
                }).ToListAsync(cancellationToken);

            return new AdminMallsPageViewModel { Malls = malls };
        }

        public async Task<AdminStoresPageViewModel> GetStoresPageAsync(Guid? editStoreId = null, CancellationToken cancellationToken = default)
        {
            List<AdminStoreListItem> stores = await GetStoreListItemsAsync(cancellationToken);
            List<SelectListItem> mallOptions = await GetMallOptionsAsync(cancellationToken);
            AdminStoreForm form = new();

            if (editStoreId.HasValue)
            {
                Store store = await _db.Stores
                    .AsNoTracking()
                    .SingleOrDefaultAsync(existingStore => existingStore.Id == editStoreId.Value, cancellationToken)
                    ?? throw new ApiNotFoundException("Store not found.", "STORE_NOT_FOUND");

                form = new AdminStoreForm
                {
                    Id = store.Id,
                    MallID = store.MallID,
                    Name = store.Name,
                    OperatingHours = store.OperatingHours,
                    SocialMediaLinksJson = store.SocialMediaLinks?.RootElement.GetRawText(),
                    Description = store.Description,
                    PhoneNumber = store.PhoneNumber,
                    Email = store.Email,
                    FloorNumber = store.FloorNumber,
                    StoreImageUrl = store.StoreImageUrl
                };
            }

            return new AdminStoresPageViewModel
            {
                Form = form,
                Stores = stores,
                MallOptions = mallOptions
            };
        }

        public async Task<AdminManagersPageViewModel> GetManagersPageAsync(Guid? editManagerId = null, CancellationToken cancellationToken = default)
        {
            AdminManagerForm form = new();

            if (editManagerId.HasValue)
                form = await GetManagerFormAsync(editManagerId.Value, cancellationToken);

            return new AdminManagersPageViewModel
            {
                Form = form,
                Managers = await GetManagerListItemsAsync(cancellationToken),
                MallOptions = await GetMallOptionsAsync(cancellationToken),
                Stores = await GetStoreListItemsAsync(cancellationToken)
            };
        }

        public async Task<Guid> CreateMallAsync(AdminMallForm form, CancellationToken cancellationToken = default)
        {
            string name = NormalizeRequired(form.Name, "Mall name is required.");
            bool exists = await _db.Malls.AnyAsync(mall => mall.Name == name, cancellationToken);
            if (exists)
                throw new ApiConflictException("A mall with this name already exists.", "MALL_ALREADY_EXISTS");

            var mall = new Mall
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Malls.Add(mall);
            await _db.SaveChangesAsync(cancellationToken);
            return mall.Id;
        }

        public async Task<Guid> CreateStoreAsync(AdminStoreForm form, CancellationToken cancellationToken = default)
        {
            await EnsureMallExistsAsync(form.MallID, cancellationToken);

            var store = new Store
            {
                Id = Guid.NewGuid(),
                MallID = form.MallID,
                Name = NormalizeRequired(form.Name, "Store name is required."),
                OperatingHours = NormalizeOptional(form.OperatingHours),
                SocialMediaLinks = ParseOptionalJson(form.SocialMediaLinksJson),
                Description = NormalizeOptional(form.Description),
                PhoneNumber = NormalizeOptional(form.PhoneNumber),
                Email = NormalizeOptional(form.Email),
                FloorNumber = NormalizeOptional(form.FloorNumber),
                StoreImageUrl = NormalizeOptional(form.StoreImageUrl)
            };

            _db.Stores.Add(store);
            await _db.SaveChangesAsync(cancellationToken);
            return store.Id;
        }

        public async Task UpdateStoreAsync(AdminStoreForm form, CancellationToken cancellationToken = default)
        {
            if (!form.Id.HasValue || form.Id.Value == Guid.Empty)
                throw new ApiValidationException("Store ID is required.", "STORE_ID_REQUIRED");

            await EnsureMallExistsAsync(form.MallID, cancellationToken);

            Store store = await _db.Stores
                .SingleOrDefaultAsync(existingStore => existingStore.Id == form.Id.Value, cancellationToken)
                ?? throw new ApiNotFoundException("Store not found.", "STORE_NOT_FOUND");

            store.MallID = form.MallID;
            store.Name = NormalizeRequired(form.Name, "Store name is required.");
            store.OperatingHours = NormalizeOptional(form.OperatingHours);
            store.SocialMediaLinks = ParseOptionalJson(form.SocialMediaLinksJson);
            store.Description = NormalizeOptional(form.Description);
            store.PhoneNumber = NormalizeOptional(form.PhoneNumber);
            store.Email = NormalizeOptional(form.Email);
            store.FloorNumber = NormalizeOptional(form.FloorNumber);
            store.StoreImageUrl = NormalizeOptional(form.StoreImageUrl);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> CreateManagerAsync(AdminManagerForm form, CancellationToken cancellationToken = default)
        {
            await DatabaseSchemaRepair.EnsureManagerUserProfileForeignKeyAsync(_db, cancellationToken);
            await EnsureMallExistsAsync(form.MallID, cancellationToken);
            List<Guid> validStoreIds = await ValidateStoreAssignmentsAsync(form.MallID, form.StoreIds, cancellationToken);
            string normalizedPhone = NormalizePhone(form.PhoneNumber);

            bool phoneExists = await _db.UserProfiles.AnyAsync(user => user.PhoneNumber == normalizedPhone, cancellationToken);
            if (phoneExists)
                throw new ApiConflictException("A user with this phone number already exists.", "PHONE_ALREADY_EXISTS");

            var manager = new Manager
            {
                Id = Guid.NewGuid(),
                MallID = form.MallID,
                Name = NormalizeRequired(form.Name, "Manager name is required."),
                Role = NormalizeOptional(form.Role) ?? "manager"
            };

            var user = new UserProfile
            {
                Id = manager.Id,
                MallID = manager.MallID,
                Name = manager.Name,
                PhoneNumber = normalizedPhone,
                Role = manager.Role,
                TotalPoints = 0
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, NormalizeRequired(form.Password, "Password is required."));

            _db.UserProfiles.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            _db.Managers.Add(manager);
            await _db.SaveChangesAsync(cancellationToken);

            _db.Management.AddRange(validStoreIds.Select(storeId => new Management
            {
                ManagerId = manager.Id,
                StoreId = storeId,
                CreatedAt = DateTimeOffset.UtcNow
            }));

            await _db.SaveChangesAsync(cancellationToken);
            return manager.Id;
        }

        public async Task UpdateManagerAsync(AdminManagerForm form, CancellationToken cancellationToken = default)
        {
            if (!form.Id.HasValue || form.Id.Value == Guid.Empty)
                throw new ApiValidationException("Manager ID is required.", "MANAGER_ID_REQUIRED");

            await EnsureMallExistsAsync(form.MallID, cancellationToken);
            List<Guid> validStoreIds = await ValidateStoreAssignmentsAsync(form.MallID, form.StoreIds, cancellationToken);
            string normalizedPhone = NormalizePhone(form.PhoneNumber);

            bool phoneExists = await _db.UserProfiles
                .AnyAsync(user => user.Id != form.Id.Value && user.PhoneNumber == normalizedPhone, cancellationToken);
            if (phoneExists)
                throw new ApiConflictException("A user with this phone number already exists.", "PHONE_ALREADY_EXISTS");

            Manager manager = await _db.Managers
                .SingleOrDefaultAsync(existingManager => existingManager.Id == form.Id.Value, cancellationToken)
                ?? throw new ApiNotFoundException("Manager not found.", "MANAGER_NOT_FOUND");

            UserProfile user = await _db.UserProfiles
                .SingleOrDefaultAsync(existingUser => existingUser.Id == form.Id.Value, cancellationToken)
                ?? throw new ApiNotFoundException("Manager login account not found.", "MANAGER_ACCOUNT_NOT_FOUND");

            string name = NormalizeRequired(form.Name, "Manager name is required.");
            string role = NormalizeOptional(form.Role) ?? "manager";

            manager.MallID = form.MallID;
            manager.Name = name;
            manager.Role = role;

            user.MallID = form.MallID;
            user.Name = name;
            user.PhoneNumber = normalizedPhone;
            user.Role = role;

            if (!string.IsNullOrWhiteSpace(form.Password))
                user.PasswordHash = _passwordHasher.HashPassword(user, form.Password);

            List<Management> existingAssignments = await _db.Management
                .Where(assignment => assignment.ManagerId == manager.Id)
                .ToListAsync(cancellationToken);

            if (existingAssignments.Count > 0)
                _db.Management.RemoveRange(existingAssignments);

            _db.Management.AddRange(validStoreIds.Select(storeId => new Management
            {
                ManagerId = manager.Id,
                StoreId = storeId,
                CreatedAt = DateTimeOffset.UtcNow
            }));

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteManagerAsync(Guid managerId, CancellationToken cancellationToken = default)
        {
            if (managerId == Guid.Empty)
                throw new ApiValidationException("Manager ID is required.", "MANAGER_ID_REQUIRED");

            Manager manager = await _db.Managers
                .SingleOrDefaultAsync(existingManager => existingManager.Id == managerId, cancellationToken)
                ?? throw new ApiNotFoundException("Manager not found.", "MANAGER_NOT_FOUND");

            await DeleteManagerGraphAsync(manager, cancellationToken);
        }

        private async Task EnsureMallExistsAsync(Guid mallId, CancellationToken cancellationToken)
        {
            if (mallId == Guid.Empty)
                throw new ApiValidationException("Mall is required.", "MALL_REQUIRED");

            bool exists = await _db.Malls.AsNoTracking().AnyAsync(mall => mall.Id == mallId, cancellationToken);
            if (!exists)
                throw new ApiValidationException("Selected mall does not exist.", "MALL_NOT_FOUND");
        }

        private async Task<List<Guid>> ValidateStoreAssignmentsAsync(Guid mallId, IEnumerable<Guid>? storeIds, CancellationToken cancellationToken)
        {
            List<Guid> requestedStoreIds = storeIds?
                .Where(storeId => storeId != Guid.Empty)
                .Distinct()
                .ToList()
                ?? [];

            if (requestedStoreIds.Count == 0)
                return [];

            List<Guid> validStoreIds = await _db.Stores
                .AsNoTracking()
                .Where(store => store.MallID == mallId && requestedStoreIds.Contains(store.Id))
                .Select(store => store.Id)
                .ToListAsync(cancellationToken);

            if (validStoreIds.Count != requestedStoreIds.Count)
                throw new ApiValidationException("One or more assigned stores do not belong to the selected mall.", "INVALID_STORE_ASSIGNMENT");

            return validStoreIds;
        }

        private async Task<List<SelectListItem>> GetMallOptionsAsync(CancellationToken cancellationToken)
            => await _db.Malls
                .AsNoTracking()
                .OrderBy(mall => mall.Name)
                .Select(mall => new SelectListItem(mall.Name, mall.Id.ToString()))
                .ToListAsync(cancellationToken);

        private async Task<List<AdminStoreListItem>> GetStoreListItemsAsync(CancellationToken cancellationToken)
            => await (
                from store in _db.Stores.AsNoTracking()
                join mall in _db.Malls.AsNoTracking() on store.MallID equals mall.Id
                orderby mall.Name, store.Name
                select new AdminStoreListItem
                {
                    Id = store.Id,
                    MallID = store.MallID,
                    MallName = mall.Name,
                    Name = store.Name,
                    FloorNumber = store.FloorNumber,
                    PhoneNumber = store.PhoneNumber,
                    Email = store.Email
                }).ToListAsync(cancellationToken);

        private async Task<List<AdminManagerListItem>> GetManagerListItemsAsync(CancellationToken cancellationToken)
        {
            var rows = await (
                from manager in _db.Managers.AsNoTracking()
                join user in _db.UserProfiles.AsNoTracking() on manager.Id equals user.Id
                join mall in _db.Malls.AsNoTracking() on manager.MallID equals mall.Id
                join assignment in _db.Management.AsNoTracking() on manager.Id equals assignment.ManagerId into assignments
                from assignment in assignments.DefaultIfEmpty()
                join store in _db.Stores.AsNoTracking() on assignment.StoreId equals store.Id into stores
                from store in stores.DefaultIfEmpty()
                orderby mall.Name, manager.Name
                select new
                {
                    manager.Id,
                    manager.MallID,
                    MallName = mall.Name,
                    manager.Name,
                    user.PhoneNumber,
                    manager.Role,
                    StoreId = store != null ? (Guid?)store.Id : null,
                    StoreName = store != null ? store.Name : null
                }).ToListAsync(cancellationToken);

            return rows
                .GroupBy(row => new { row.Id, row.MallID, row.MallName, row.Name, row.PhoneNumber, row.Role })
                .Select(group => new AdminManagerListItem
                {
                    Id = group.Key.Id,
                    MallID = group.Key.MallID,
                    MallName = group.Key.MallName,
                    Name = group.Key.Name,
                    PhoneNumber = group.Key.PhoneNumber,
                    Role = group.Key.Role,
                    AssignedStoreIds = group
                        .Select(row => row.StoreId)
                        .Where(storeId => storeId.HasValue)
                        .Select(storeId => storeId!.Value)
                        .Distinct()
                        .ToList(),
                    AssignedStoreNames = group
                        .Select(row => row.StoreName)
                        .Where(storeName => !string.IsNullOrWhiteSpace(storeName))
                        .Select(storeName => storeName!)
                        .OrderBy(storeName => storeName)
                        .ToList()
                })
                .ToList();
        }

        private async Task<PortalDashboardReportViewModel> BuildDashboardReportAsync(
            Guid currentUserId,
            string scopeTitle,
            string? period,
            bool canManageMallOperations,
            CancellationToken cancellationToken)
        {
            string normalizedPeriod = NormalizeDashboardPeriod(period);
            var query = CreateDashboardDateRange(normalizedPeriod);
            DashboardSummaryResponse summary = await _dashboardService.GetSummaryAsync(currentUserId, query, cancellationToken);
            DashboardSalesResponse sales = await _dashboardService.GetSalesAsync(currentUserId, query, cancellationToken);
            DashboardPointsResponse points = await _dashboardService.GetPointsAsync(currentUserId, query, cancellationToken);
            DashboardCouponsResponse coupons = await _dashboardService.GetCouponsAsync(currentUserId, query, cancellationToken);
            DashboardActivityResponse activity = await _dashboardService.GetActivityAsync(currentUserId, query, cancellationToken);

            return new PortalDashboardReportViewModel
            {
                CanManageMallOperations = canManageMallOperations,
                ScopeTitle = scopeTitle,
                Period = normalizedPeriod,
                PeriodTitle = GetDashboardPeriodTitle(normalizedPeriod),
                Summary = summary,
                Sales = sales,
                Points = points,
                Coupons = coupons,
                Activity = activity,
                SalesChart = BuildChart(
                    sales.DailySales.GroupBy(point => GetDashboardBucket(point.Date, normalizedPeriod))
                        .Select(group => new DashboardChartPoint { Label = group.Key, Value = group.Sum(point => point.SalesAmount) })),
                TransactionsChart = BuildChart(
                    sales.DailySales.GroupBy(point => GetDashboardBucket(point.Date, normalizedPeriod))
                        .Select(group => new DashboardChartPoint { Label = group.Key, Value = group.Sum(point => point.TransactionsCount) })),
                PointsIssuedChart = BuildChart(
                    points.DailyIssued.GroupBy(point => GetDashboardBucket(point.Date, normalizedPeriod))
                        .Select(group => new DashboardChartPoint { Label = group.Key, Value = group.Sum(point => point.PointsIssued) })),
                PointsRedeemedChart = BuildChart(
                    points.DailyRedeemed.GroupBy(point => GetDashboardBucket(point.Date, normalizedPeriod))
                        .Select(group => new DashboardChartPoint { Label = group.Key, Value = group.Sum(point => point.PointsRedeemed) }))
            };
        }

        private static string NormalizeDashboardPeriod(string? period)
            => string.Equals(period, "monthly", StringComparison.OrdinalIgnoreCase)
                ? "monthly"
                : string.Equals(period, "yearly", StringComparison.OrdinalIgnoreCase)
                    ? "yearly"
                    : "daily";

        private static DTOs.Dashboard.DashboardDateRangeQuery CreateDashboardDateRange(string period)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset from = period switch
            {
                "monthly" => new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "yearly" => new DateTimeOffset(now.Year - 4, 1, 1, 0, 0, 0, TimeSpan.Zero),
                _ => new DateTimeOffset(now.UtcDateTime.Date.AddDays(-6), TimeSpan.Zero)
            };

            return new DTOs.Dashboard.DashboardDateRangeQuery { From = from, To = now };
        }

        private static string GetDashboardPeriodTitle(string period)
            => period switch
            {
                "monthly" => "Monthly report",
                "yearly" => "Yearly report",
                _ => "Daily report"
            };

        private static string GetDashboardBucket(DateTime date, string period)
            => period switch
            {
                "monthly" => date.ToString("MMM"),
                "yearly" => date.ToString("yyyy"),
                _ => date.ToString("MMM d")
            };

        private static IReadOnlyList<DashboardChartPoint> BuildChart(IEnumerable<DashboardChartPoint> points)
        {
            List<DashboardChartPoint> chartPoints = points.ToList();
            decimal max = chartPoints.Count == 0 ? 0 : chartPoints.Max(point => point.Value);

            foreach (DashboardChartPoint point in chartPoints)
                point.Percent = max <= 0 ? 0 : Math.Round(point.Value / max * 100, 2);

            return chartPoints;
        }

        private async Task<AdminManagerForm> GetManagerFormAsync(Guid managerId, CancellationToken cancellationToken)
        {
            var manager = await (
                from existingManager in _db.Managers.AsNoTracking()
                join user in _db.UserProfiles.AsNoTracking() on existingManager.Id equals user.Id
                where existingManager.Id == managerId
                select new
                {
                    existingManager.Id,
                    existingManager.MallID,
                    existingManager.Name,
                    user.PhoneNumber,
                    existingManager.Role
                }).SingleOrDefaultAsync(cancellationToken)
                ?? throw new ApiNotFoundException("Manager not found.", "MANAGER_NOT_FOUND");

            List<Guid> storeIds = await _db.Management
                .AsNoTracking()
                .Where(assignment => assignment.ManagerId == managerId)
                .Select(assignment => assignment.StoreId)
                .ToListAsync(cancellationToken);

            return new AdminManagerForm
            {
                Id = manager.Id,
                MallID = manager.MallID,
                Name = manager.Name,
                PhoneNumber = manager.PhoneNumber,
                Role = manager.Role,
                StoreIds = storeIds
            };
        }

        private async Task DeleteManagerGraphAsync(Manager manager, CancellationToken cancellationToken)
        {
            List<UserSession> sessions = await _db.UserSessions
                .Where(session => session.UserId == manager.Id)
                .ToListAsync(cancellationToken);
            List<Management> assignments = await _db.Management
                .Where(assignment => assignment.ManagerId == manager.Id)
                .ToListAsync(cancellationToken);
            List<Coupon> coupons = await _db.Coupons
                .Where(coupon => coupon.ManagerId == manager.Id)
                .ToListAsync(cancellationToken);
            List<Announcement> announcements = await _db.Announcements
                .Where(announcement => announcement.ManagerId == manager.Id)
                .ToListAsync(cancellationToken);
            UserProfile? user = await _db.UserProfiles
                .SingleOrDefaultAsync(profile => profile.Id == manager.Id, cancellationToken);

            _db.UserSessions.RemoveRange(sessions);
            _db.Management.RemoveRange(assignments);
            _db.Coupons.RemoveRange(coupons);
            _db.Announcements.RemoveRange(announcements);
            _db.Managers.Remove(manager);

            if (user != null)
                _db.UserProfiles.Remove(user);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private static JsonDocument? ParseOptionalJson(string? value)
        {
            string? normalized = NormalizeOptional(value);
            if (normalized == null)
                return null;

            try
            {
                return JsonDocument.Parse(normalized);
            }
            catch (JsonException ex)
            {
                throw new ApiValidationException($"Social media links must be valid JSON: {ex.Message}", "INVALID_JSON");
            }
        }

        private static string NormalizeRequired(string? value, string message)
        {
            string? normalized = NormalizeOptional(value);
            if (normalized == null)
                throw new ApiValidationException(message, "VALUE_REQUIRED");

            return normalized;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private string NormalizePhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ApiValidationException("Phone number is required.", "PHONE_REQUIRED");

            try
            {
                return _phoneNumberService.Normalize(phoneNumber);
            }
            catch (ArgumentException ex)
            {
                throw new ApiValidationException(ex.Message, "INVALID_PHONE_NUMBER");
            }
        }
    }
}
