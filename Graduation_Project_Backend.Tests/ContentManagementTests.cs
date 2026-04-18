using Graduation_Project_Backend.DTOs.Announcements;
using Graduation_Project_Backend.DTOs.Offers;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Graduation_Project_Backend.Tests
{
    public sealed class ContentManagementTests
    {
        [Fact]
        public async Task CreateOfferAsync_StoreScopedManagerOutsideAssignedStore_ThrowsForbidden()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();
            Guid storeAId = Guid.NewGuid();
            Guid storeBId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile { Id = managerId, Name = "Manager", PhoneNumber = "+962700000008", PasswordHash = "hash", Role = "manager", MallID = mallId });
            db.Managers.Add(new Manager { Id = managerId, Name = "Manager", Role = "manager", MallID = mallId });
            db.Stores.AddRange(
                new Store { Id = storeAId, Name = "Store A", MallID = mallId },
                new Store { Id = storeBId, Name = "Store B", MallID = mallId });
            db.Management.Add(new Management { ManagerId = managerId, StoreId = storeAId, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var offersService = new OffersService(db, accessService, NullLogger<OffersService>.Instance);

            await Assert.ThrowsAsync<ApiForbiddenException>(() => offersService.CreateOfferAsync(managerId, new CreateOfferRequest
            {
                StoreId = storeBId,
                Title = "Offer",
                StartAt = DateTimeOffset.UtcNow,
                EndAt = DateTimeOffset.UtcNow.AddDays(1)
            }));
        }

        [Fact]
        public async Task UpdateAnnouncementAsync_MallWideManager_UpdatesAnnouncement()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();
            Guid announcementId = Guid.NewGuid();

            db.Malls.Add(new Mall { Id = mallId, Name = "City Mall", CreatedAt = DateTimeOffset.UtcNow });
            db.UserProfiles.Add(new UserProfile { Id = managerId, Name = "Manager", PhoneNumber = "+962700000009", PasswordHash = "hash", Role = "manager", MallID = mallId });
            db.Managers.Add(new Manager { Id = managerId, Name = "Manager", Role = "manager", MallID = mallId });
            db.Announcements.Add(new Announcement
            {
                Id = announcementId,
                MallID = mallId,
                ManagerId = managerId,
                Title = "Old",
                Content = "Old Content",
                AnnouncementType = "general",
                Priority = "normal",
                IsActive = true,
                IsPinned = false,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var accessService = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
            var service = new AnnouncementsService(db, accessService, NullLogger<AnnouncementsService>.Instance);

            AnnouncementResponse response = await service.UpdateAnnouncementAsync(managerId, announcementId, new UpdateAnnouncementRequest
            {
                Title = "Updated",
                Content = "Updated Content",
                AnnouncementType = "news",
                Priority = "high",
                IsActive = true,
                IsPinned = true,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(2)
            });

            Assert.Equal("Updated", response.Title);
            Assert.True(response.IsPinned);
        }
    }
}
