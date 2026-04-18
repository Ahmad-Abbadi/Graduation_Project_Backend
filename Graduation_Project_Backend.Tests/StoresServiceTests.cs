using Graduation_Project_Backend.DTOs.Stores;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Graduation_Project_Backend.Tests
{
    public sealed class StoresServiceTests
    {
        [Fact]
        public async Task CreateStoreAsync_MallWideManager_CreatesStoreAndCategories()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();
            long categoryAId = 1;
            long categoryBId = 2;

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile
            {
                Id = managerId,
                Name = "Mall Manager",
                PhoneNumber = "+962700000001",
                PasswordHash = "hash",
                Role = "manager",
                MallID = mallId
            });
            db.Managers.Add(new Manager { Id = managerId, Name = "Mall Manager", Role = "manager", MallID = mallId });
            db.Categories.AddRange(
                new Category { Id = categoryAId, Name = "Fashion", MallID = mallId },
                new Category { Id = categoryBId, Name = "Sports", MallID = mallId });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var service = new StoresService(db, accessService, NullLogger<StoresService>.Instance);

            StoreResponse response = await service.CreateStoreAsync(managerId, new CreateStoreRequest
            {
                Name = " Nike ",
                OperatingHours = "9 AM - 10 PM",
                CategoryIds = [categoryAId, categoryBId]
            });

            Assert.Equal("Nike", response.Name);
            Assert.Equal(mallId, response.MallID);
            Assert.Equal(2, response.Categories.Count);
            Assert.Equal(1, db.Stores.Count(store => store.Name == "Nike"));
            Assert.Equal(2, db.StoreCategories.Count(storeCategory => storeCategory.StoreId == response.Id));
        }

        [Fact]
        public async Task CreateStoreAsync_StoreScopedManager_ThrowsForbidden()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();
            Guid storeId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile
            {
                Id = managerId,
                Name = "Store Manager",
                PhoneNumber = "+962700000002",
                PasswordHash = "hash",
                Role = "manager",
                MallID = mallId
            });
            db.Managers.Add(new Manager { Id = managerId, Name = "Store Manager", Role = "manager", MallID = mallId });
            db.Stores.Add(new Store { Id = storeId, Name = "Assigned Store", MallID = mallId });
            db.Management.Add(new Management { ManagerId = managerId, StoreId = storeId, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var service = new StoresService(db, accessService, NullLogger<StoresService>.Instance);

            await Assert.ThrowsAsync<ApiForbiddenException>(() => service.CreateStoreAsync(managerId, new CreateStoreRequest
            {
                Name = "New Store"
            }));
        }
    }
}
