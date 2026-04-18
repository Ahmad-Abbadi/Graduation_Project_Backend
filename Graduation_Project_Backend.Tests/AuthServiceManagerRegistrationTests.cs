using Graduation_Project_Backend.DTOs.Auth;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Auth;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Session;
using Graduation_Project_Backend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;

namespace Graduation_Project_Backend.Tests
{
    public sealed class AuthServiceManagerRegistrationTests
    {
        [Fact]
        public async Task RegisterAsync_WithManagerId_CreatesManagerLinkedUserProfile()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();

            db.Malls.Add(new Mall
            {
                Id = mallId,
                Name = "City Mall",
                CreatedAt = DateTimeOffset.UtcNow
            });

            db.Managers.Add(new Manager
            {
                Id = managerId,
                Name = "Qais",
                Role = "manager",
                MallID = mallId
            });

            await db.SaveChangesAsync();

            var authService = new AuthService(
                db,
                new PhoneNumberService(),
                new PasswordHasher<UserProfile>(),
                new SessionService(db));

            var response = await authService.RegisterAsync(new RegisterRequestDto
            {
                ManagerId = managerId,
                PhoneNumber = "0791234567",
                Password = "123456",
                MallID = mallId
            });

            UserProfile user = Assert.Single(db.UserProfiles);
            Assert.Equal(managerId, user.Id);
            Assert.Equal("Qais", user.Name);
            Assert.Equal("manager", user.Role);
            Assert.Equal("+962791234567", user.PhoneNumber);
            Assert.Equal(managerId, response.UserId);
            Assert.Equal("manager", response.Role);
            Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
        }

        [Fact]
        public async Task ManagerQuickLoginAsync_WithManagerId_CreatesLinkedUserProfileAndSession()
        {
            using AppDbContext db = TestInfrastructure.CreateDbContext();
            Guid mallId = Guid.NewGuid();
            Guid managerId = Guid.NewGuid();

            db.Malls.Add(new Mall
            {
                Id = mallId,
                Name = "City Mall",
                CreatedAt = DateTimeOffset.UtcNow
            });

            db.Managers.Add(new Manager
            {
                Id = managerId,
                Name = "Qais",
                Role = "manager",
                MallID = mallId
            });

            await db.SaveChangesAsync();

            var authService = new AuthService(
                db,
                new PhoneNumberService(),
                new PasswordHasher<UserProfile>(),
                new SessionService(db));

            var response = await authService.ManagerQuickLoginAsync(new ManagerQuickLoginRequestDto
            {
                ManagerId = managerId
            });

            UserProfile user = Assert.Single(db.UserProfiles);
            Assert.Equal(managerId, user.Id);
            Assert.Equal("Qais", user.Name);
            Assert.Equal("manager", user.Role);
            Assert.Equal(mallId, user.MallID);
            Assert.StartsWith("manager-", user.PhoneNumber);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.Equal(managerId, response.UserId);
            Assert.Equal("manager", response.Role);
            Assert.False(string.IsNullOrWhiteSpace(response.SessionId));
            Assert.Single(db.UserSessions);
        }
    }
}
