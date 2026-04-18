using Graduation_Project_Backend.DTOs.Dashboard;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Tests.TestSupport;

namespace Graduation_Project_Backend.Tests
{
    public sealed class DashboardServiceTests
    {
        [Fact]
        public async Task GetSummaryAsync_StoreScopedManager_OnlyCountsAssignedStoreTransactions()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();
            Guid storeAId = Guid.NewGuid();
            Guid storeBId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile { Id = managerId, Name = "Manager", PhoneNumber = "+962700000010", PasswordHash = "hash", Role = "manager", MallID = mallId });
            db.Managers.Add(new Manager { Id = managerId, Name = "Manager", Role = "manager", MallID = mallId });
            db.Stores.AddRange(
                new Store { Id = storeAId, Name = "Store A", MallID = mallId },
                new Store { Id = storeBId, Name = "Store B", MallID = mallId });
            db.Management.Add(new Management { ManagerId = managerId, StoreId = storeAId, CreatedAt = DateTimeOffset.UtcNow });
            db.Transactions.AddRange(
                new Transaction { Id = 1, UserId = Guid.NewGuid(), StoreId = storeAId, ReceiptId = "a-1", Price = 10, Points = 1000, CreatedAt = DateTimeOffset.UtcNow, TransactionStatus = "completed" },
                new Transaction { Id = 2, UserId = Guid.NewGuid(), StoreId = storeBId, ReceiptId = "b-1", Price = 20, Points = 2000, CreatedAt = DateTimeOffset.UtcNow, TransactionStatus = "completed" });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<UserAccessService>.Instance);
            var service = new DashboardService(db, accessService);

            DashboardSummaryResponse response = await service.GetSummaryAsync(managerId, new DashboardDateRangeQuery());

            Assert.Equal(1, response.TotalTransactions);
            Assert.Equal(10, response.TotalSalesAmount);
            Assert.Equal(1000, response.TotalPointsIssued);
            Assert.Null(response.ActivatedCouponsCount);
        }
    }
}
