using System.Security.Cryptography;
using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service
{
    public sealed class ServiceClass
    {
        private readonly AppDbContext _db;

        public ServiceClass(AppDbContext db)
        {
            _db = db;
        }

        public string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phone));

            string cleaned = phone.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(".", "");

            bool hasPlus = cleaned.StartsWith("+");

            if (hasPlus)
                cleaned = cleaned.Substring(1);

            if (!cleaned.All(char.IsDigit))
                throw new ArgumentException("Phone number contains invalid characters.", nameof(phone));

            if (string.IsNullOrEmpty(cleaned))
                throw new ArgumentException("Phone number must contain digits.", nameof(phone));

            if (hasPlus)
            {
                if (!cleaned.StartsWith("962"))
                    throw new ArgumentException("Only Jordanian phone numbers are accepted. Expected format: +9627XXXXXXXX", nameof(phone));

                if (cleaned.Length != 12)
                    throw new ArgumentException($"Invalid Jordanian phone number length. Expected 12 digits (9627XXXXXXXX), got {cleaned.Length}.", nameof(phone));

                if (cleaned[3] != '7')
                    throw new ArgumentException("Invalid Jordanian mobile number. Expected format: +9627XXXXXXXX", nameof(phone));

                return "+" + cleaned;
            }
            else
            {
                if (cleaned.StartsWith("07"))
                {
                    if (cleaned.Length != 10)
                        throw new ArgumentException($"Invalid Jordanian mobile number. Expected 10 digits (07XXXXXXXX), got {cleaned.Length}.", nameof(phone));

                    return "+962" + cleaned.Substring(1);
                }
                else if (cleaned.StartsWith("962"))
                {
                    if (cleaned.Length != 12)
                        throw new ArgumentException($"Invalid phone number. Expected 12 digits (9627XXXXXXXX), got {cleaned.Length}.", nameof(phone));

                    if (cleaned[3] != '7')
                        throw new ArgumentException("Invalid Jordanian mobile number. Expected format: 9627XXXXXXXX", nameof(phone));

                    return "+" + cleaned;
                }
                else
                {
                    throw new ArgumentException("Invalid phone number format. Expected Jordanian format: 07XXXXXXXX or +9627XXXXXXXX", nameof(phone));
                }
            }
        }

        public async Task<string> GenerateUniqueSerialAsync()
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

        public async Task<UserProfile?> GetUserByPhoneAndMallIDAsync(string phone, Guid mallID)
        {
            return await _db.UserProfiles
                .SingleOrDefaultAsync(u => u.PhoneNumber == phone && mallID == u.MallID);
        }

        public async Task<UserProfile?> GetUserByIdAsync(Guid userId)
        {
            return await _db.UserProfiles
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public void AddPoints(UserProfile user, int points)
        {
            user.TotalPoints += points;
        }

        public void DeductPoints(UserProfile user, int points)
        {
            if (user.TotalPoints < points)
                throw new InvalidOperationException("Not enough points");

            user.TotalPoints -= points;
        }

        public async Task<UserProfile> CreateUserAsync(UserProfile user)
        {
            _db.UserProfiles.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<Store?> GetStoreByIdAsync(Guid storeId)
        {
            return await _db.Stores
                .SingleOrDefaultAsync(s => s.Id == storeId);
        }
        public async Task<Store> CreateStoreAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Store name is required");

            var store = new Store
            {
                Id = Guid.NewGuid(),
                Name = name.Trim()
            };

            _db.Stores.Add(store);
            await _db.SaveChangesAsync();

            return store;
        }
        public async Task<bool> ReceiptExistsAsync(string receiptId)
        {
            return await _db.Transactions
                .AnyAsync(t => t.ReceiptId == receiptId);
        }

        public async Task<Transaction> CreateTransactionAsync(
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

            return transaction;
        }
        public async Task<Guid?> GetMallIdByStoreIdAsync(Guid storeId)
        {
            var store = await _db.Stores
                .Where(s => s.Id == storeId)
                .Select(s => s.MallID)
                .FirstOrDefaultAsync();

            return store;
        }
        public async Task<TransactionResultDto> ProcessTransactionAsync(
            string phoneNumber,
            Guid storeId,
            string receiptId,
            string? receiptDescription,
            decimal price
        )
        {
            if (await ReceiptExistsAsync(receiptId))
                throw new InvalidOperationException("Receipt ID already exists");
            var mallId = await GetMallIdByStoreIdAsync(storeId);
            var user = await GetUserByPhoneAndMallIDAsync(phoneNumber, mallId.Value)
                ?? throw new InvalidOperationException("User not found");

            var transaction = await CreateTransactionAsync(
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
                var now = DateTime.UtcNow;
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

        public async Task<Coupon?> GetCouponAsync(Guid couponId)
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

            var now = DateTime.UtcNow;
            if (coupon.StartAt > now || coupon.EndAt < now)
                throw new InvalidOperationException("Coupon outside redeem period");

            var user = await GetUserByIdAsync(userId)
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

            var now = DateTime.UtcNow;
            if (userCoupon.Coupon.StartAt > now || userCoupon.Coupon.EndAt < now)
                throw new InvalidOperationException("Coupon outside redeem period");

            userCoupon.IsRedeemed = true;
            await _db.SaveChangesAsync();

            return userCoupon;
        }

        public async Task<List<UserCoupon>> GetUserCouponsAsync(Guid userId)
        {
            return await _db.UserCoupons
                .Include(uc => uc.Coupon)
                .Where(uc => uc.UserId == userId)
                .OrderByDescending(uc => uc.CreatedAt)
                .ToListAsync();
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
    }
}
