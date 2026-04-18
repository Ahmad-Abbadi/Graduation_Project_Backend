using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs.Dashboard;
using Graduation_Project_Backend.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;
        private readonly IUserAccessService _userAccessService;

        public DashboardService(AppDbContext db, IUserAccessService userAccessService)
        {
            _db = db;
            _userAccessService = userAccessService;
        }

        public async Task<DashboardSummaryResponse> GetSummaryAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            List<TransactionMetricRow> transactions = await GetTransactionMetricsAsync(access, query, cancellationToken);
            CouponMetricSnapshot? coupons = await GetCouponSnapshotAsync(access, query, cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            int activeOffers = await FilterOffersByScope(access, _db.Offers.AsNoTracking())
                .CountAsync(offer => offer.IsActive && offer.StartAt <= now && offer.EndAt >= now, cancellationToken);

            int activeAnnouncements = await FilterAnnouncementsByScope(access, _db.Announcements.AsNoTracking())
                .CountAsync(announcement => announcement.IsActive && announcement.StartDate <= now && announcement.EndDate >= now, cancellationToken);

            return new DashboardSummaryResponse
            {
                TotalTransactions = transactions.Count,
                TotalSalesAmount = transactions.Sum(transaction => transaction.Price),
                TotalPointsIssued = transactions.Sum(transaction => transaction.Points),
                TotalPointsRedeemed = coupons?.PointsRedeemed,
                ActiveOffersCount = activeOffers,
                ActiveAnnouncementsCount = activeAnnouncements,
                RedeemedCouponsCount = coupons?.RedeemedCoupons,
                ActivatedCouponsCount = coupons?.ActivatedCoupons
            };
        }

        public async Task<DashboardSalesResponse> GetSalesAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            List<TransactionMetricRow> transactions = await GetTransactionMetricsAsync(access, query, cancellationToken);

            IReadOnlyList<DailySalesPointResponse> dailySales = transactions
                .GroupBy(transaction => transaction.CreatedAt.UtcDateTime.Date)
                .OrderBy(group => group.Key)
                .Select(group => new DailySalesPointResponse
                {
                    Date = group.Key,
                    SalesAmount = group.Sum(transaction => transaction.Price),
                    TransactionsCount = group.Count()
                })
                .ToList();

            IReadOnlyList<StoreSalesPointResponse> topStores = transactions
                .GroupBy(transaction => new { transaction.StoreId, transaction.StoreName })
                .OrderByDescending(group => group.Sum(transaction => transaction.Price))
                .Take(5)
                .Select(group => new StoreSalesPointResponse
                {
                    StoreId = group.Key.StoreId,
                    StoreName = group.Key.StoreName,
                    SalesAmount = group.Sum(transaction => transaction.Price),
                    TransactionsCount = group.Count()
                })
                .ToList();

            return new DashboardSalesResponse
            {
                TotalSalesAmount = transactions.Sum(transaction => transaction.Price),
                TotalTransactions = transactions.Count,
                DailySales = dailySales,
                TopStores = topStores
            };
        }

        public async Task<DashboardPointsResponse> GetPointsAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            List<TransactionMetricRow> transactions = await GetTransactionMetricsAsync(access, query, cancellationToken);
            CouponMetricSnapshot? coupons = await GetCouponSnapshotAsync(access, query, cancellationToken);

            IReadOnlyList<DailyIssuedPointsResponse> dailyIssued = transactions
                .GroupBy(transaction => transaction.CreatedAt.UtcDateTime.Date)
                .OrderBy(group => group.Key)
                .Select(group => new DailyIssuedPointsResponse
                {
                    Date = group.Key,
                    PointsIssued = group.Sum(transaction => transaction.Points)
                })
                .ToList();

            IReadOnlyList<DailyRedeemedPointsResponse> dailyRedeemed = coupons?.DailyRedeemed
                .GroupBy(item => item.Date)
                .OrderBy(group => group.Key)
                .Select(group => new DailyRedeemedPointsResponse
                {
                    Date = group.Key,
                    PointsRedeemed = group.Sum(item => item.PointsRedeemed)
                })
                .ToList()
                ?? [];

            return new DashboardPointsResponse
            {
                TotalPointsIssued = transactions.Sum(transaction => transaction.Points),
                TotalPointsRedeemed = coupons?.PointsRedeemed,
                DailyIssued = dailyIssued,
                DailyRedeemed = dailyRedeemed
            };
        }

        public async Task<DashboardCouponsResponse> GetCouponsAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            CouponMetricSnapshot? coupons = await GetCouponSnapshotAsync(access, query, cancellationToken);
            if (coupons == null)
            {
                return new DashboardCouponsResponse
                {
                    IsScopeLimited = true
                };
            }

            return new DashboardCouponsResponse
            {
                IsScopeLimited = false,
                TotalActiveCoupons = coupons.ActiveCoupons,
                TotalActivatedUserCoupons = coupons.ActivatedCoupons,
                TotalRedeemedCoupons = coupons.RedeemedCoupons,
                RedemptionRate = coupons.ActivatedCoupons == 0
                    ? 0
                    : Math.Round((decimal)coupons.RedeemedCoupons / coupons.ActivatedCoupons, 4)
            };
        }

        public async Task<DashboardActivityResponse> GetActivityAsync(Guid currentUserId, DashboardDateRangeQuery query, CancellationToken cancellationToken = default)
        {
            UserAccessContext access = await GetManagerAccessAsync(currentUserId, cancellationToken);
            List<TransactionMetricRow> transactions = await GetTransactionMetricsAsync(access, query, cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            int totalOffers = await FilterOffersByScope(access, _db.Offers.AsNoTracking()).CountAsync(cancellationToken);
            int totalAnnouncements = await FilterAnnouncementsByScope(access, _db.Announcements.AsNoTracking()).CountAsync(cancellationToken);
            int activeOffers = await FilterOffersByScope(access, _db.Offers.AsNoTracking())
                .CountAsync(offer => offer.IsActive && offer.StartAt <= now && offer.EndAt >= now, cancellationToken);
            int activeAnnouncements = await FilterAnnouncementsByScope(access, _db.Announcements.AsNoTracking())
                .CountAsync(announcement => announcement.IsActive && announcement.StartDate <= now && announcement.EndDate >= now, cancellationToken);

            int? unreadNotifications = null;
            if (access.IsMallWideManager)
            {
                unreadNotifications = await (
                    from notification in _db.Notifications.AsNoTracking()
                    join user in _db.UserProfiles.AsNoTracking() on notification.UserId equals user.Id
                    where user.MallID == access.MallID && !notification.IsRead
                    select notification.Id
                ).CountAsync(cancellationToken);
            }

            IReadOnlyList<CategoryDistributionResponse> categoryDistribution = await GetCategoryDistributionAsync(access, cancellationToken);

            return new DashboardActivityResponse
            {
                TotalOffers = totalOffers,
                TotalAnnouncements = totalAnnouncements,
                ActiveOffers = activeOffers,
                ActiveAnnouncements = activeAnnouncements,
                UnreadNotifications = unreadNotifications,
                RecentTransactions = transactions
                    .OrderByDescending(transaction => transaction.CreatedAt)
                    .Take(10)
                    .Select(transaction => new RecentTransactionActivityResponse
                    {
                        TransactionId = transaction.TransactionId,
                        StoreId = transaction.StoreId,
                        StoreName = transaction.StoreName,
                        ReceiptId = transaction.ReceiptId,
                        Price = transaction.Price,
                        Points = transaction.Points,
                        CreatedAt = transaction.CreatedAt,
                        Status = transaction.Status
                    })
                    .ToList(),
                CategoryDistribution = categoryDistribution
            };
        }

        private async Task<UserAccessContext> GetManagerAccessAsync(Guid currentUserId, CancellationToken cancellationToken)
        {
            UserAccessContext access = await _userAccessService.GetUserAccessContextAsync(currentUserId, cancellationToken);
            if (!access.IsManager)
                throw new ApiForbiddenException("Only managers can access dashboard analytics.", "MANAGER_REQUIRED");

            return access;
        }

        private IQueryable<Models.Entities.Offer> FilterOffersByScope(UserAccessContext access, IQueryable<Models.Entities.Offer> query)
            => access.IsMallWideManager
                ? query.Where(offer => offer.MallID == access.MallID)
                : query.Where(offer => access.AssignedStoreIds.Contains(offer.StoreId));

        private IQueryable<Models.Entities.Announcement> FilterAnnouncementsByScope(UserAccessContext access, IQueryable<Models.Entities.Announcement> query)
            => access.IsMallWideManager
                ? query.Where(announcement => announcement.MallID == access.MallID)
                : query.Where(announcement => announcement.StoreId.HasValue && access.AssignedStoreIds.Contains(announcement.StoreId.Value));

        private IQueryable<TransactionMetricRow> BuildTransactionMetricsQuery(UserAccessContext access)
            => from transaction in _db.Transactions.AsNoTracking()
               join store in _db.Stores.AsNoTracking() on transaction.StoreId equals store.Id
               where access.IsMallWideManager ? store.MallID == access.MallID : access.AssignedStoreIds.Contains(store.Id)
               select new TransactionMetricRow(
                   transaction.Id,
                   transaction.StoreId,
                   store.Name,
                   transaction.ReceiptId,
                   transaction.Price,
                   transaction.Points,
                   transaction.CreatedAt,
                   transaction.TransactionStatus);

        private async Task<List<TransactionMetricRow>> GetTransactionMetricsAsync(
            UserAccessContext access,
            DashboardDateRangeQuery query,
            CancellationToken cancellationToken)
        {
            ValidateDateRange(query);

            IQueryable<TransactionMetricRow> transactions = BuildTransactionMetricsQuery(access);

            if (query.From.HasValue)
                transactions = transactions.Where(transaction => transaction.CreatedAt >= query.From.Value);

            if (query.To.HasValue)
                transactions = transactions.Where(transaction => transaction.CreatedAt <= query.To.Value);

            return await transactions.ToListAsync(cancellationToken);
        }

        private async Task<CouponMetricSnapshot?> GetCouponSnapshotAsync(
            UserAccessContext access,
            DashboardDateRangeQuery query,
            CancellationToken cancellationToken)
        {
            ValidateDateRange(query);

            if (!access.IsMallWideManager)
                return null;

            DateTimeOffset now = DateTimeOffset.UtcNow;

            int activeCoupons = await _db.Coupons.AsNoTracking()
                .CountAsync(coupon =>
                    coupon.MallID == access.MallID &&
                    coupon.IsActive &&
                    coupon.StartAt <= now &&
                    coupon.EndAt >= now,
                    cancellationToken);

            var userCouponsQuery =
                from userCoupon in _db.UserCoupons.AsNoTracking()
                join coupon in _db.Coupons.AsNoTracking() on userCoupon.CouponId equals coupon.Id
                where coupon.MallID == access.MallID
                select new
                {
                    userCoupon.CreatedAt,
                    userCoupon.IsRedeemed,
                    CostPoint = coupon.CostPoint ?? 0
                };

            if (query.From.HasValue)
                userCouponsQuery = userCouponsQuery.Where(item => item.CreatedAt >= query.From.Value.UtcDateTime);

            if (query.To.HasValue)
                userCouponsQuery = userCouponsQuery.Where(item => item.CreatedAt <= query.To.Value.UtcDateTime);

            List<CouponMetricRow> rows = await userCouponsQuery
                .Select(item => new CouponMetricRow(item.CreatedAt.Date, item.IsRedeemed, item.CostPoint))
                .ToListAsync(cancellationToken);

            return new CouponMetricSnapshot
            {
                ActiveCoupons = activeCoupons,
                ActivatedCoupons = rows.Count,
                RedeemedCoupons = rows.Count(row => row.IsRedeemed),
                PointsRedeemed = rows.Sum(row => row.CostPoint),
                DailyRedeemed = rows
                    .GroupBy(row => row.Date)
                    .Select(group => new DailyRedeemedRow(group.Key, group.Sum(item => item.CostPoint)))
                    .ToList()
            };
        }

        private async Task<IReadOnlyList<CategoryDistributionResponse>> GetCategoryDistributionAsync(UserAccessContext access, CancellationToken cancellationToken)
        {
            IQueryable<Guid> scopedStoreIds = access.IsMallWideManager
                ? _db.Stores.AsNoTracking().Where(store => store.MallID == access.MallID).Select(store => store.Id)
                : _db.Stores.AsNoTracking().Where(store => access.AssignedStoreIds.Contains(store.Id)).Select(store => store.Id);

            return await (
                from storeCategory in _db.StoreCategories.AsNoTracking()
                join category in _db.Categories.AsNoTracking() on storeCategory.CategoryId equals category.Id
                where scopedStoreIds.Contains(storeCategory.StoreId)
                group storeCategory by new { category.Id, category.Name } into categoryGroup
                orderby categoryGroup.Count() descending, categoryGroup.Key.Name
                select new CategoryDistributionResponse
                {
                    CategoryId = categoryGroup.Key.Id,
                    CategoryName = categoryGroup.Key.Name,
                    StoresCount = categoryGroup.Count()
                }).ToListAsync(cancellationToken);
        }

        private static void ValidateDateRange(DashboardDateRangeQuery query)
        {
            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
                throw new ApiValidationException("The from date must be earlier than the to date.", "INVALID_DATE_RANGE");
        }

        private sealed record TransactionMetricRow(
            long TransactionId,
            Guid StoreId,
            string StoreName,
            string ReceiptId,
            decimal Price,
            int Points,
            DateTimeOffset CreatedAt,
            string? Status);

        private sealed record CouponMetricRow(DateTime Date, bool IsRedeemed, decimal CostPoint);
        private sealed record DailyRedeemedRow(DateTime Date, decimal PointsRedeemed);

        private sealed class CouponMetricSnapshot
        {
            public int ActiveCoupons { get; init; }
            public int ActivatedCoupons { get; init; }
            public int RedeemedCoupons { get; init; }
            public decimal PointsRedeemed { get; init; }
            public IReadOnlyList<DailyRedeemedRow> DailyRedeemed { get; init; } = [];
        }
    }
}
