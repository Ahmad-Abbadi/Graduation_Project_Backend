using Graduation_Project_Backend.DTOs.Receipts;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Realtime;
using Graduation_Project_Backend.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Graduation_Project_Backend.Tests
{
    public sealed class RewardsServiceReceiptTests
    {
        [Fact]
        public async Task GetReceiptDetailsForUserAsync_NonOwnerCustomer_ThrowsForbidden()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid ownerId = Guid.NewGuid();
            Guid otherUserId = Guid.NewGuid();
            Guid storeId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.AddRange(
                new UserProfile { Id = ownerId, Name = "Owner", PhoneNumber = "+962700000003", PasswordHash = "hash", Role = "user", MallID = mallId },
                new UserProfile { Id = otherUserId, Name = "Other", PhoneNumber = "+962700000004", PasswordHash = "hash", Role = "user", MallID = mallId });
            db.Stores.Add(new Store { Id = storeId, Name = "Nike", MallID = mallId });
            db.Transactions.Add(new Transaction
            {
                Id = 1,
                UserId = ownerId,
                StoreId = storeId,
                ReceiptId = "r-1",
                ReceiptDescription = "Receipt",
                Price = 15,
                Points = 1500,
                TransactionStatus = "completed",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var rewardsService = new RewardsService(
                db,
                new PhoneNumberService(),
                new NoOpUserPointsUpdatesService(),
                accessService);

            await Assert.ThrowsAsync<ApiForbiddenException>(() => rewardsService.GetReceiptDetailsForUserAsync(otherUserId, 1));
        }

        [Fact]
        public async Task GetMyReceiptsAsync_ReturnsOnlyCurrentUsersReceipts()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid otherUserId = Guid.NewGuid();
            Guid storeId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.AddRange(
                new UserProfile { Id = userId, Name = "User", PhoneNumber = "+962700000005", PasswordHash = "hash", Role = "user", MallID = mallId },
                new UserProfile { Id = otherUserId, Name = "Other", PhoneNumber = "+962700000006", PasswordHash = "hash", Role = "user", MallID = mallId });
            db.Stores.Add(new Store { Id = storeId, Name = "Nike", MallID = mallId });
            db.Transactions.AddRange(
                new Transaction { Id = 1, UserId = userId, StoreId = storeId, ReceiptId = "mine", ReceiptDescription = "Mine", Price = 10, Points = 1000, TransactionStatus = "completed", CreatedAt = DateTimeOffset.UtcNow },
                new Transaction { Id = 2, UserId = otherUserId, StoreId = storeId, ReceiptId = "other", ReceiptDescription = "Other", Price = 20, Points = 2000, TransactionStatus = "completed", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var rewardsService = new RewardsService(
                db,
                new PhoneNumberService(),
                new NoOpUserPointsUpdatesService(),
                accessService);

            var result = await rewardsService.GetMyReceiptsAsync(userId, new ReceiptListQuery());

            Assert.Single(result.Items);
            Assert.Equal("mine", result.Items[0].ReceiptId);
        }
    }
}
