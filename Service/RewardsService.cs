using System.Security.Cryptography;
using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Realtime;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class RewardsService : IRewardsService
    {
        private readonly AppDbContext _db;
        private readonly IPhoneNumberService _phoneNumberService;
        private readonly IUserPointsUpdatesService _userPointsUpdatesService;

        public RewardsService(
            AppDbContext db,
            IPhoneNumberService phoneNumberService,
            IUserPointsUpdatesService userPointsUpdatesService)
        {
            _db = db;
            _phoneNumberService = phoneNumberService;
            _userPointsUpdatesService = userPointsUpdatesService;
        }

        private string NormalizePhone(string phone)
            => _phoneNumberService.Normalize(phone);

        private async Task<string> GenerateUniqueSerialAsync()
        {
            string serial;
            do
            {
                serial = GenerateSerialNumber();
            }
            while (await _db.UserCoupons.AnyAsync(x => x.SerialNumber == serial));

            return serial;
        }

        private string GenerateSerialNumber()
        {
            const int length = 8;
            Span<byte> bytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(bytes);

            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = (char)('0' + (bytes[i] % 10));

            return new string(result);
        }

        private async Task<UserProfile?> GetUserByPhoneAndMallIdAsync(string phone, Guid mallId)
        {
            return await _db.UserProfiles
                .SingleOrDefaultAsync(user => user.PhoneNumber == phone && user.MallID == mallId);
        }

        private async Task<UserProfile?> GetUserByIdAsync(Guid userId)
        {
            return await _db.UserProfiles
                .SingleOrDefaultAsync(user => user.Id == userId);
        }

        private static void AddPoints(UserProfile user, int points)
        {
            user.TotalPoints += points;
        }

        private static void DeductPoints(UserProfile user, int points)
        {
            if (user.TotalPoints < points)
                throw new InvalidOperationException("Not enough points");

            user.TotalPoints -= points;
        }

        private async Task<bool> ReceiptExistsAsync(string receiptId)
        {
            return await _db.Transactions
                .AnyAsync(t => t.ReceiptId == receiptId);
        }

        private async Task<Transaction> CreateTransactionAsync(
            UserProfile user,
            Guid storeId,
            string receiptId,
            string? description,
            decimal price
        )
        {
            var points = CalculatePoints(price);

            var transaction = new Transaction
            {
                UserId = user.Id,
                StoreId = storeId,
                ReceiptId = receiptId,
                ReceiptDescription = description ?? "",
                Price = price,
                Points = points,
                CreatedAt = DateTimeOffset.UtcNow
            };

            AddPoints(user, points);

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();
            await PublishPointsChangedAsync(user.Id, user.TotalPoints, "transaction");

            return transaction;
        }

        private async Task<Guid?> GetMallIdByStoreIdAsync(Guid storeId)
        {
            return await _db.Stores
                .Where(s => s.Id == storeId)
                .Select(store => (Guid?)store.MallID)
                .FirstOrDefaultAsync();
        }

        public async Task<TransactionResultDto> ProcessTransactionAsync(
            string phoneNumber,
            Guid storeId,
            string receiptId,
            string? receiptDescription,
            decimal price
        )
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new InvalidOperationException("Phone number is required");

            if (storeId == Guid.Empty)
                throw new InvalidOperationException("Store ID is required");

            if (string.IsNullOrWhiteSpace(receiptId))
                throw new InvalidOperationException("Receipt ID is required");

            if (price < 0)
                throw new InvalidOperationException("Price cannot be negative");

            if (await ReceiptExistsAsync(receiptId))
                throw new InvalidOperationException("Receipt ID already exists");

            Guid? mallId = await GetMallIdByStoreIdAsync(storeId);
            if (mallId == null)
                throw new InvalidOperationException("Store not found");

            string normalizedPhone = NormalizePhone(phoneNumber);
            UserProfile user = await GetUserByPhoneAndMallIdAsync(normalizedPhone, mallId.Value)
                ?? throw new InvalidOperationException("User not found");

            Transaction transaction = await CreateTransactionAsync(
                user,
                storeId,
                receiptId,
                receiptDescription,
                price
            );

            return new TransactionResultDto
            {
                TransactionId = transaction.Id,
                UserId = user.Id,
                StoreId = transaction.StoreId,
                ReceiptId = transaction.ReceiptId,
                Price = transaction.Price,
                Points = transaction.Points,
                NewTotalPoints = user.TotalPoints,
                CreatedAt = transaction.CreatedAt
            };
        }

        public async Task<object?> GetTransactionDetailsAsync(long transactionId)
        {
            var transaction = await _db.Transactions
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return null;

            return new
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                UserName = transaction.User?.Name,
                PhoneNumber = transaction.User?.PhoneNumber,
                StoreId = transaction.StoreId,
                ReceiptId = transaction.ReceiptId,
                ReceiptDescription = transaction.ReceiptDescription,
                Price = transaction.Price,
                Points = transaction.Points,
                CreatedAt = transaction.CreatedAt
            };
        }

        private static int CalculatePoints(decimal price)
            => (int)(price * 100);

        public async Task<List<Coupon>> GetCouponsAsync(bool? isActive)
        {
            var query = _db.Coupons.AsQueryable();

            if (isActive.HasValue)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                query = query.Where(c =>
                    c.IsActive == isActive.Value &&
                    c.StartAt <= now &&
                    c.EndAt >= now
                );
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<object?> GetCouponDetailsAsync(Guid couponId)
        {
            var coupon = await _db.Coupons
                .SingleOrDefaultAsync(c => c.Id == couponId);

            if (coupon == null)
                return null;

            return new
            {
                Id = coupon.Id,
                Type = coupon.Type,
                Description = coupon.Discription,
                StartAt = coupon.StartAt,
                EndAt = coupon.EndAt,
                IsActive = coupon.IsActive,
                CostPoint = coupon.CostPoint,
                CreatedAt = coupon.CreatedAt,
                ManagerId = coupon.ManagerId
            };
        }

        private async Task<Coupon?> GetCouponAsync(Guid couponId)
        {
            return await _db.Coupons
                .SingleOrDefaultAsync(c => c.Id == couponId);
        }

        public async Task<UserCoupon> RedeemCouponAsync(Guid userId, Guid couponId)
        {
            var coupon = await GetCouponAsync(couponId)
                ?? throw new InvalidOperationException("Coupon not found");

            if (!coupon.IsActive)
                throw new InvalidOperationException("Coupon is not active");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (coupon.StartAt > now || coupon.EndAt < now)
                throw new InvalidOperationException("Coupon outside redeem period");

            UserProfile user = await GetUserByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found");

            using var tx = await _db.Database.BeginTransactionAsync();

            if (coupon.CostPoint.HasValue)
                DeductPoints(user, (int)coupon.CostPoint.Value);

            var serial = await GenerateUniqueSerialAsync();

            var userCoupon = new UserCoupon
            {
                SerialNumber = serial,
                UserId = userId,
                CouponId = couponId,
                IsRedeemed = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.UserCoupons.Add(userCoupon);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await PublishPointsChangedAsync(user.Id, user.TotalPoints, "coupon_redeem");

            return userCoupon;
        }

        public async Task<UserCoupon> RedeemCouponBySerialAsync(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new InvalidOperationException("Serial number is required");

            var serial = serialNumber.Trim();

            var userCoupon = await _db.UserCoupons
                .Include(uc => uc.Coupon)
                .SingleOrDefaultAsync(uc => uc.SerialNumber == serial);

            if (userCoupon == null)
                throw new InvalidOperationException("Coupon serial not found");

            if (userCoupon.IsRedeemed)
                throw new InvalidOperationException("Coupon already redeemed");

            if (userCoupon.Coupon == null)
                throw new InvalidOperationException("Coupon not found");

            if (!userCoupon.Coupon.IsActive)
                throw new InvalidOperationException("Coupon is not active");

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (userCoupon.Coupon.StartAt > now || userCoupon.Coupon.EndAt < now)
                throw new InvalidOperationException("Coupon outside redeem period");

            userCoupon.IsRedeemed = true;
            await _db.SaveChangesAsync();

            return userCoupon;
        }

        public async Task<List<object>> GetUserCouponsViewAsync(Guid userId)
        {
            var userCoupons = await _db.UserCoupons
                .Include(uc => uc.Coupon)
                .Where(uc => uc.UserId == userId)
                .OrderByDescending(uc => uc.CreatedAt)
                .ToListAsync();

            return userCoupons.Select(uc => new
            {
                SerialNumber = uc.SerialNumber,
                CouponId = uc.CouponId,
                CouponType = uc.Coupon?.Type,
                CouponDescription = uc.Coupon?.Discription,
                IsRedeemed = uc.IsRedeemed,
                ValidFrom = uc.Coupon?.StartAt,
                ValidUntil = uc.Coupon?.EndAt,
                CreatedAt = uc.CreatedAt
            })
            .Cast<object>()
            .ToList();
        }

        public async Task<int?> GetUserTotalPointsAsync(Guid userId)
        {
            var user = await _db.UserProfiles
                .Where(u => u.Id == userId)
                .Select(u => new { u.TotalPoints })
                .SingleOrDefaultAsync();

            return user?.TotalPoints;
        }

        private ValueTask PublishPointsChangedAsync(Guid userId, int totalPoints, string source)
            => _userPointsUpdatesService.PublishAsync(userId, totalPoints, source);
    }
}
