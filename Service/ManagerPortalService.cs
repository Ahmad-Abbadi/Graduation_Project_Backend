using System.Text.Json;
using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Announcements;
using Graduation_Project_Backend.DTOs.Coupons;
using Graduation_Project_Backend.DTOs.Dashboard;
using Graduation_Project_Backend.DTOs.Offers;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Models.ViewModels;
using Graduation_Project_Backend.Service.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class ManagerPortalService : IManagerPortalService
    {
        private readonly AppDbContext _db;
        private readonly IUserAccessService _userAccessService;
        private readonly IPhoneNumberService _phoneNumberService;
        private readonly IPasswordHasher<UserProfile> _passwordHasher;
        private readonly IOffersService _offersService;
        private readonly IRewardsService _rewardsService;
        private readonly IAnnouncementsService _announcementsService;
        private readonly IDashboardService _dashboardService;

        public ManagerPortalService(
            AppDbContext db,
            IUserAccessService userAccessService,
            IPhoneNumberService phoneNumberService,
            IPasswordHasher<UserProfile> passwordHasher,
            IOffersService offersService,
            IRewardsService rewardsService,
            IAnnouncementsService announcementsService,
            IDashboardService dashboardService)
        {
            _db = db;
            _userAccessService = userAccessService;
            _phoneNumberService = phoneNumberService;
            _passwordHasher = passwordHasher;
            _offersService = offersService;
            _rewardsService = rewardsService;
            _announcementsService = announcementsService;
            _dashboardService = dashboardService;
        }

        public async Task<ManagerDashboardViewModel> GetDashboardAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            var query = new DashboardDateRangeQuery();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string mallName = await _db.Malls.AsNoTracking()
                .Where(mall => mall.Id == access.MallID)
                .Select(mall => mall.Name)
                .SingleOrDefaultAsync(cancellationToken) ?? "Mall";

            return new ManagerDashboardViewModel
            {
                CanManageMallOperations = access.IsMallWideManager,
                MallName = mallName,
                StoresCount = await ScopedStores(access).CountAsync(cancellationToken),
                ManagersCount = access.IsMallWideManager
                    ? await _db.Managers.AsNoTracking().CountAsync(manager => manager.MallID == access.MallID, cancellationToken)
                    : 0,
                ActiveOffersCount = await ScopedOffers(access)
                    .CountAsync(offer => offer.IsActive && offer.StartAt <= now && offer.EndAt >= now, cancellationToken),
                ActiveCouponsCount = access.IsMallWideManager
                    ? await _db.Coupons.AsNoTracking().CountAsync(coupon => coupon.MallID == access.MallID && coupon.IsActive && coupon.StartAt <= now && coupon.EndAt >= now, cancellationToken)
                    : 0,
                ActiveAnnouncementsCount = access.IsMallWideManager
                    ? await _db.Announcements.AsNoTracking().CountAsync(announcement => announcement.MallID == access.MallID && announcement.IsActive && announcement.StartDate <= now && announcement.EndDate >= now, cancellationToken)
                    : 0,
                Summary = await _dashboardService.GetSummaryAsync(currentUserId, query, cancellationToken),
                Sales = await _dashboardService.GetSalesAsync(currentUserId, query, cancellationToken),
                Points = await _dashboardService.GetPointsAsync(currentUserId, query, cancellationToken),
                Coupons = await _dashboardService.GetCouponsAsync(currentUserId, query, cancellationToken),
                Activity = await _dashboardService.GetActivityAsync(currentUserId, query, cancellationToken)
            };
        }

        public async Task<PortalDashboardReportViewModel> GetDashboardReportAsync(Guid currentUserId, string? period, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            string mallName = await _db.Malls.AsNoTracking()
                .Where(mall => mall.Id == access.MallID)
                .Select(mall => mall.Name)
                .SingleOrDefaultAsync(cancellationToken) ?? "Mall";

            string scopeTitle = access.IsMallWideManager ? mallName : $"{mallName} assigned stores";
            return await BuildDashboardReportAsync(currentUserId, scopeTitle, period, access.IsMallWideManager, cancellationToken);
        }

        public async Task<ManagerStoresPageViewModel> GetStoresPageAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);
            return new ManagerStoresPageViewModel
            {
                CanManageMallOperations = access.IsMallWideManager,
                Stores = await GetStoreListItemsAsync(access, cancellationToken)
            };
        }

        public async Task<ManagerStoreManagersPageViewModel> GetStoreManagersPageAsync(Guid currentUserId, Guid? editManagerId = null, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);
            AdminManagerForm form = new();

            if (editManagerId.HasValue)
                form = await GetStoreManagerFormAsync(access, editManagerId.Value, cancellationToken);

            return new ManagerStoreManagersPageViewModel
            {
                CanManageMallOperations = access.IsMallWideManager,
                Form = form,
                Managers = await GetManagerListItemsAsync(access, cancellationToken),
                Stores = await GetStoreListItemsAsync(access, cancellationToken)
            };
        }

        public async Task<ManagerOffersPageViewModel> GetOffersPageAsync(Guid currentUserId, long? editOfferId = null, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            ManagerOfferForm form = new();

            if (editOfferId.HasValue)
            {
                Offer offer = await GetScopedOfferAsync(access, editOfferId.Value, cancellationToken);
                form = new ManagerOfferForm
                {
                    Id = offer.Id,
                    StoreId = offer.StoreId,
                    Title = offer.Title,
                    Description = offer.Description,
                    StartAt = offer.StartAt,
                    EndAt = offer.EndAt,
                    IsActive = offer.IsActive
                };
            }

            return new ManagerOffersPageViewModel
            {
                CanManageMallOperations = access.IsMallWideManager,
                Form = form,
                Offers = await _offersService.GetManagedOffersAsync(currentUserId, cancellationToken),
                StoreOptions = await GetStoreOptionsAsync(access, cancellationToken)
            };
        }

        public async Task<ManagerCouponsPageViewModel> GetCouponsPageAsync(Guid currentUserId, Guid? editCouponId = null, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);
            ManagerCouponForm form = new();

            if (editCouponId.HasValue)
            {
                Coupon coupon = await GetScopedCouponAsync(access, editCouponId.Value, cancellationToken);
                form = new ManagerCouponForm
                {
                    Id = coupon.Id,
                    Type = coupon.Type,
                    Description = coupon.Discription,
                    StartAt = coupon.StartAt,
                    EndAt = coupon.EndAt,
                    IsActive = coupon.IsActive,
                    CostPoint = coupon.CostPoint
                };
            }

            List<Coupon> coupons = await _db.Coupons.AsNoTracking()
                .Where(coupon => coupon.MallID == access.MallID)
                .OrderByDescending(coupon => coupon.CreatedAt)
                .ToListAsync(cancellationToken);

            return new ManagerCouponsPageViewModel
            {
                CanManageMallOperations = access.IsMallWideManager,
                Form = form,
                Coupons = coupons
            };
        }

        public async Task<ManagerAnnouncementsPageViewModel> GetAnnouncementsPageAsync(Guid currentUserId, Guid? editAnnouncementId = null, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);
            ManagerAnnouncementForm form = new();

            if (editAnnouncementId.HasValue)
            {
                Announcement announcement = await GetScopedAnnouncementAsync(access, editAnnouncementId.Value, cancellationToken);
                form = new ManagerAnnouncementForm
                {
                    Id = announcement.Id,
                    StoreId = announcement.StoreId,
                    Title = announcement.Title,
                    Content = announcement.Content,
                    AnnouncementType = announcement.AnnouncementType,
                    Priority = announcement.Priority,
                    IsActive = announcement.IsActive,
                    IsPinned = announcement.IsPinned,
                    ImageUrl = announcement.ImageUrl,
                    StartDate = announcement.StartDate,
                    EndDate = announcement.EndDate
                };
            }

            return new ManagerAnnouncementsPageViewModel
            {
                CanManageMallOperations = access.IsMallWideManager,
                Form = form,
                Announcements = await _announcementsService.GetManagedAnnouncementsAsync(currentUserId, cancellationToken),
                StoreOptions = await GetStoreOptionsAsync(access, cancellationToken)
            };
        }

        public async Task<Guid> CreateStoreAsync(Guid currentUserId, AdminStoreForm form, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);
            var store = new Store
            {
                Id = Guid.NewGuid(),
                MallID = access.MallID,
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
            if (!access.IsMallWideManager)
            {
                _db.Management.Add(new Management
                {
                    ManagerId = currentUserId,
                    StoreId = store.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return store.Id;
        }

        public async Task<Guid> CreateStoreManagerAsync(Guid currentUserId, AdminManagerForm form, CancellationToken cancellationToken = default)
        {
            await DatabaseSchemaRepair.EnsureManagerUserProfileForeignKeyAsync(_db, cancellationToken);
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);
            List<Guid> validStoreIds = await ValidateStoreAssignmentsAsync(access, form.StoreIds, requireAssignments: true, cancellationToken);
            string normalizedPhone = NormalizePhone(form.PhoneNumber);
            bool phoneExists = await _db.UserProfiles.AnyAsync(user => user.PhoneNumber == normalizedPhone, cancellationToken);
            if (phoneExists)
                throw new ApiConflictException("A user with this phone number already exists.", "PHONE_ALREADY_EXISTS");

            var manager = new Manager
            {
                Id = Guid.NewGuid(),
                MallID = access.MallID,
                Name = NormalizeRequired(form.Name, "Manager name is required."),
                Role = "manager"
            };

            var user = new UserProfile
            {
                Id = manager.Id,
                MallID = access.MallID,
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

        public async Task UpdateStoreManagerAsync(Guid currentUserId, AdminManagerForm form, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            if (!form.Id.HasValue || form.Id.Value == Guid.Empty)
                throw new ApiValidationException("Manager ID is required.", "MANAGER_ID_REQUIRED");

            List<Guid> validStoreIds = await ValidateStoreAssignmentsAsync(access, form.StoreIds, requireAssignments: true, cancellationToken);
            string normalizedPhone = NormalizePhone(form.PhoneNumber);
            bool phoneExists = await _db.UserProfiles
                .AnyAsync(user => user.Id != form.Id.Value && user.PhoneNumber == normalizedPhone, cancellationToken);
            if (phoneExists)
                throw new ApiConflictException("A user with this phone number already exists.", "PHONE_ALREADY_EXISTS");

            Manager manager = await GetEditableStoreManagerAsync(access, form.Id.Value, cancellationToken);
            UserProfile user = await _db.UserProfiles
                .SingleOrDefaultAsync(existingUser => existingUser.Id == manager.Id, cancellationToken)
                ?? throw new ApiNotFoundException("Manager login account not found.", "MANAGER_ACCOUNT_NOT_FOUND");

            string name = NormalizeRequired(form.Name, "Manager name is required.");

            manager.Name = name;
            manager.Role = "manager";
            manager.MallID = access.MallID;

            user.Name = name;
            user.PhoneNumber = normalizedPhone;
            user.Role = "manager";
            user.MallID = access.MallID;

            if (!string.IsNullOrWhiteSpace(form.Password))
                user.PasswordHash = _passwordHasher.HashPassword(user, form.Password);

            List<Management> existingAssignments = await _db.Management
                .Where(assignment => assignment.ManagerId == manager.Id)
                .ToListAsync(cancellationToken);

            _db.Management.RemoveRange(existingAssignments);
            _db.Management.AddRange(validStoreIds.Select(storeId => new Management
            {
                ManagerId = manager.Id,
                StoreId = storeId,
                CreatedAt = DateTimeOffset.UtcNow
            }));

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteStoreManagerAsync(Guid currentUserId, Guid managerId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            Manager manager = await GetEditableStoreManagerAsync(access, managerId, cancellationToken);
            await DeleteManagerGraphAsync(manager, cancellationToken);
        }

        public async Task CreateOfferAsync(Guid currentUserId, ManagerOfferForm form, CancellationToken cancellationToken = default)
            => await _offersService.CreateOfferAsync(currentUserId, new CreateOfferRequest
            {
                StoreId = form.StoreId,
                Title = form.Title,
                Description = form.Description,
                StartAt = form.StartAt,
                EndAt = form.EndAt,
                IsActive = form.IsActive
            }, cancellationToken);

        public async Task UpdateOfferAsync(Guid currentUserId, ManagerOfferForm form, CancellationToken cancellationToken = default)
        {
            if (!form.Id.HasValue)
                throw new ApiValidationException("Offer ID is required.", "OFFER_ID_REQUIRED");

            await _offersService.UpdateOfferAsync(currentUserId, form.Id.Value, new UpdateOfferRequest
            {
                StoreId = form.StoreId,
                Title = form.Title,
                Description = form.Description,
                StartAt = form.StartAt,
                EndAt = form.EndAt,
                IsActive = form.IsActive
            }, cancellationToken);
        }

        public async Task DeleteOfferAsync(Guid currentUserId, long offerId, CancellationToken cancellationToken = default)
            => await _offersService.DeleteOfferAsync(currentUserId, offerId, cancellationToken);

        public async Task CreateCouponAsync(Guid currentUserId, ManagerCouponForm form, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            await _rewardsService.CreateCouponAsync(currentUserId, new CreateCouponRequest
            {
                Type = form.Type,
                Description = form.Description,
                StartAt = form.StartAt,
                EndAt = form.EndAt,
                IsActive = form.IsActive,
                CostPoint = form.CostPoint
            }, cancellationToken);
        }

        public async Task UpdateCouponAsync(Guid currentUserId, ManagerCouponForm form, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            if (!form.Id.HasValue)
                throw new ApiValidationException("Coupon ID is required.", "COUPON_ID_REQUIRED");

            if (form.StartAt >= form.EndAt)
                throw new ApiValidationException("Start date must be earlier than end date.", "INVALID_DATE_RANGE");

            Coupon coupon = await GetScopedCouponAsync(access, form.Id.Value, cancellationToken);
            coupon.Type = NormalizeRequired(form.Type, "Coupon type is required.");
            coupon.Discription = NormalizeOptional(form.Description) ?? string.Empty;
            coupon.StartAt = form.StartAt;
            coupon.EndAt = form.EndAt;
            coupon.IsActive = form.IsActive;
            coupon.CostPoint = form.CostPoint;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteCouponAsync(Guid currentUserId, Guid couponId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            Coupon coupon = await GetScopedCouponAsync(access, couponId, cancellationToken);
            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateAnnouncementAsync(Guid currentUserId, ManagerAnnouncementForm form, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            await _announcementsService.CreateAnnouncementAsync(currentUserId, new CreateAnnouncementRequest
            {
                StoreId = form.StoreId,
                Title = form.Title,
                Content = form.Content,
                AnnouncementType = form.AnnouncementType,
                Priority = form.Priority,
                IsActive = form.IsActive,
                IsPinned = form.IsPinned,
                ImageUrl = form.ImageUrl,
                StartDate = form.StartDate,
                EndDate = form.EndDate
            }, cancellationToken);
        }

        public async Task UpdateAnnouncementAsync(Guid currentUserId, ManagerAnnouncementForm form, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            if (!form.Id.HasValue)
                throw new ApiValidationException("Announcement ID is required.", "ANNOUNCEMENT_ID_REQUIRED");

            await _announcementsService.UpdateAnnouncementAsync(currentUserId, form.Id.Value, new UpdateAnnouncementRequest
            {
                StoreId = form.StoreId,
                Title = form.Title,
                Content = form.Content,
                AnnouncementType = form.AnnouncementType,
                Priority = form.Priority,
                IsActive = form.IsActive,
                IsPinned = form.IsPinned,
                ImageUrl = form.ImageUrl,
                StartDate = form.StartDate,
                EndDate = form.EndDate
            }, cancellationToken);
        }

        public async Task DeleteAnnouncementAsync(Guid currentUserId, Guid announcementId, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            EnsureMallWideManager(access);

            await _announcementsService.DeleteAnnouncementAsync(currentUserId, announcementId, cancellationToken);
        }

        private async Task<UserAccessContext> GetManagerAccessAsync(Guid currentUserId, CancellationToken cancellationToken)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            if (!access.IsManager)
                throw new ApiForbiddenException("Only managers can access this page.", "MANAGER_REQUIRED");

            return access;
        }

        private static void EnsureMallWideManager(UserAccessContext access)
        {
            if (!access.IsMallWideManager)
                throw new ApiForbiddenException("Only mall-wide managers can access this mall operation.", "MALL_WIDE_MANAGER_REQUIRED");
        }

        private IQueryable<Store> ScopedStores(UserAccessContext access)
            => access.IsMallWideManager
                ? _db.Stores.AsNoTracking().Where(store => store.MallID == access.MallID)
                : _db.Stores.AsNoTracking().Where(store => access.AssignedStoreIds.Contains(store.Id));

        private IQueryable<Offer> ScopedOffers(UserAccessContext access)
            => access.IsMallWideManager
                ? _db.Offers.AsNoTracking().Where(offer => offer.MallID == access.MallID)
                : _db.Offers.AsNoTracking().Where(offer => access.AssignedStoreIds.Contains(offer.StoreId));

        private async Task<List<AdminStoreListItem>> GetStoreListItemsAsync(UserAccessContext access, CancellationToken cancellationToken)
            => await (
                from store in ScopedStores(access)
                join mall in _db.Malls.AsNoTracking() on store.MallID equals mall.Id
                orderby store.Name
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

        private async Task<List<SelectListItem>> GetStoreOptionsAsync(UserAccessContext access, CancellationToken cancellationToken)
            => await ScopedStores(access)
                .OrderBy(store => store.Name)
                .Select(store => new SelectListItem(store.Name, store.Id.ToString()))
                .ToListAsync(cancellationToken);

        private async Task<Offer> GetScopedOfferAsync(UserAccessContext access, long offerId, CancellationToken cancellationToken)
            => await ScopedOffers(access)
                .SingleOrDefaultAsync(offer => offer.Id == offerId, cancellationToken)
                ?? throw new ApiNotFoundException("Offer not found.", "OFFER_NOT_FOUND");

        private async Task<Coupon> GetScopedCouponAsync(UserAccessContext access, Guid couponId, CancellationToken cancellationToken)
            => await _db.Coupons
                .SingleOrDefaultAsync(coupon => coupon.Id == couponId && coupon.MallID == access.MallID, cancellationToken)
                ?? throw new ApiNotFoundException("Coupon not found.", "COUPON_NOT_FOUND");

        private async Task<Announcement> GetScopedAnnouncementAsync(UserAccessContext access, Guid announcementId, CancellationToken cancellationToken)
            => await _db.Announcements
                .AsNoTracking()
                .SingleOrDefaultAsync(announcement => announcement.Id == announcementId && announcement.MallID == access.MallID, cancellationToken)
                ?? throw new ApiNotFoundException("Announcement not found.", "ANNOUNCEMENT_NOT_FOUND");

        private async Task<List<AdminManagerListItem>> GetManagerListItemsAsync(UserAccessContext access, CancellationToken cancellationToken)
        {
            var rows = await (
                from manager in _db.Managers.AsNoTracking()
                join user in _db.UserProfiles.AsNoTracking() on manager.Id equals user.Id
                join mall in _db.Malls.AsNoTracking() on manager.MallID equals mall.Id
                join assignment in _db.Management.AsNoTracking() on manager.Id equals assignment.ManagerId
                join store in ScopedStores(access) on assignment.StoreId equals store.Id
                where manager.MallID == access.MallID
                orderby manager.Name
                select new
                {
                    manager.Id,
                    manager.MallID,
                    MallName = mall.Name,
                    manager.Name,
                    user.PhoneNumber,
                    manager.Role,
                    StoreId = store.Id,
                    StoreName = store.Name
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
                    AssignedStoreIds = group.Select(row => row.StoreId).Distinct().OrderBy(id => id).ToList(),
                    AssignedStoreNames = group.Select(row => row.StoreName).OrderBy(name => name).ToList()
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

        private static DashboardDateRangeQuery CreateDashboardDateRange(string period)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset from = period switch
            {
                "monthly" => new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "yearly" => new DateTimeOffset(now.Year - 4, 1, 1, 0, 0, 0, TimeSpan.Zero),
                _ => new DateTimeOffset(now.UtcDateTime.Date.AddDays(-6), TimeSpan.Zero)
            };

            return new DashboardDateRangeQuery { From = from, To = now };
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

        private async Task<AdminManagerForm> GetStoreManagerFormAsync(UserAccessContext access, Guid managerId, CancellationToken cancellationToken)
        {
            Manager manager = await GetEditableStoreManagerAsync(access, managerId, cancellationToken);
            UserProfile user = await _db.UserProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(existingUser => existingUser.Id == manager.Id, cancellationToken)
                ?? throw new ApiNotFoundException("Manager login account not found.", "MANAGER_ACCOUNT_NOT_FOUND");

            List<Guid> storeIds = await _db.Management
                .AsNoTracking()
                .Where(assignment => assignment.ManagerId == manager.Id)
                .Select(assignment => assignment.StoreId)
                .ToListAsync(cancellationToken);

            return new AdminManagerForm
            {
                Id = manager.Id,
                MallID = manager.MallID,
                Name = manager.Name,
                PhoneNumber = user.PhoneNumber,
                Role = manager.Role,
                StoreIds = storeIds
            };
        }

        private async Task<Manager> GetEditableStoreManagerAsync(UserAccessContext access, Guid managerId, CancellationToken cancellationToken)
        {
            Manager manager = await _db.Managers
                .SingleOrDefaultAsync(existingManager => existingManager.Id == managerId && existingManager.MallID == access.MallID, cancellationToken)
                ?? throw new ApiNotFoundException("Store manager not found.", "MANAGER_NOT_FOUND");

            bool hasAssignments = await _db.Management
                .AnyAsync(assignment => assignment.ManagerId == manager.Id, cancellationToken);
            if (!hasAssignments)
                throw new ApiForbiddenException("Mall-wide managers cannot be edited from the store managers page.", "STORE_MANAGER_REQUIRED");

            return manager;
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

        private async Task<List<Guid>> ValidateStoreAssignmentsAsync(
            UserAccessContext access,
            IEnumerable<Guid>? storeIds,
            bool requireAssignments,
            CancellationToken cancellationToken)
        {
            List<Guid> requestedStoreIds = storeIds?.Where(storeId => storeId != Guid.Empty).Distinct().ToList() ?? [];
            if (requireAssignments && requestedStoreIds.Count == 0)
                throw new ApiValidationException("Store managers must be assigned to at least one store.", "STORE_ASSIGNMENT_REQUIRED");

            if (requestedStoreIds.Count == 0)
                return [];

            List<Guid> allowedStoreIds = await ScopedStores(access)
                .Where(store => requestedStoreIds.Contains(store.Id))
                .Select(store => store.Id)
                .ToListAsync(cancellationToken);

            if (allowedStoreIds.Count != requestedStoreIds.Count)
                throw new ApiValidationException("One or more assigned stores are outside your scope.", "INVALID_STORE_ASSIGNMENT");

            return allowedStoreIds;
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

        private static string NormalizeRequired(string? value, string message)
        {
            string? normalized = NormalizeOptional(value);
            if (normalized == null)
                throw new ApiValidationException(message, "VALUE_REQUIRED");

            return normalized;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
