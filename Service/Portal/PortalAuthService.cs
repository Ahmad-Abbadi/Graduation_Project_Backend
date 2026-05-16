using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Auth;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Session;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service.Portal
{
    public sealed class PortalAuthService : IPortalAuthService
    {
        private const string DefaultAdminMallName = "Default Admin Mall";

        private readonly AppDbContext _db;
        private readonly IPhoneNumberService _phoneNumberService;
        private readonly IPasswordHasher<UserProfile> _passwordHasher;
        private readonly ISessionService _sessionService;

        public PortalAuthService(
            AppDbContext db,
            IPhoneNumberService phoneNumberService,
            IPasswordHasher<UserProfile> passwordHasher,
            ISessionService sessionService)
        {
            _db = db;
            _phoneNumberService = phoneNumberService;
            _passwordHasher = passwordHasher;
            _sessionService = sessionService;
        }

        public async Task<PortalLoginResult> LoginAsync(PortalLoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new AuthValidationException("Phone number is required.", "PHONE_REQUIRED");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new AuthValidationException("Password is required.", "PASSWORD_REQUIRED");

            string normalizedPhone;
            try
            {
                normalizedPhone = _phoneNumberService.Normalize(request.PhoneNumber);
            }
            catch (ArgumentException ex)
            {
                throw new AuthValidationException(ex.Message, "INVALID_PHONE_NUMBER");
            }

            UserProfile user = await _db.UserProfiles
                .SingleOrDefaultAsync(existingUser => existingUser.PhoneNumber == normalizedPhone, cancellationToken)
                ?? throw new AuthUnauthorizedException("Invalid phone number or password.", "INVALID_CREDENTIALS");

            PasswordVerificationResult verificationResult =
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
                throw new AuthUnauthorizedException("Invalid phone number or password.", "INVALID_CREDENTIALS");

            if (!IsPortalRole(user.Role))
                throw new ApiForbiddenException("This account cannot access the admin or manager portal.", "PORTAL_ACCESS_DENIED");

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            UserSession session = await _sessionService.CreateOrReplaceSessionAsync(user.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new PortalLoginResult
            {
                SessionId = session.Id,
                UserId = user.Id,
                Name = user.Name,
                Role = user.Role
            };
        }

        public async Task<PortalLoginResult> RegisterAdminAsync(PortalRegisterAdminRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new AuthValidationException("Name is required.", "NAME_REQUIRED");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new AuthValidationException("Password is required.", "PASSWORD_REQUIRED");

            string normalizedPhone = NormalizePhone(request.PhoneNumber);
            bool phoneExists = await _db.UserProfiles.AnyAsync(user => user.PhoneNumber == normalizedPhone, cancellationToken);
            if (phoneExists)
                throw new AuthConflictException("A user with this phone number already exists.", "PHONE_ALREADY_EXISTS");

            Guid mallId = await GetOrCreateAdminMallIdAsync(cancellationToken);
            var admin = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                PhoneNumber = normalizedPhone,
                Role = PortalConstants.AdminRole,
                TotalPoints = 0,
                MallID = mallId
            };

            admin.PasswordHash = _passwordHasher.HashPassword(admin, request.Password);

            _db.UserProfiles.Add(admin);
            UserSession session = await _sessionService.CreateOrReplaceSessionAsync(admin.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new PortalLoginResult
            {
                SessionId = session.Id,
                UserId = admin.Id,
                Name = admin.Name,
                Role = admin.Role
            };
        }

        public async Task<PortalAccountRequest> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            UserProfile user = await _db.UserProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(profile => profile.Id == userId, cancellationToken)
                ?? throw new ApiNotFoundException("Account not found.", "ACCOUNT_NOT_FOUND");

            if (!IsPortalRole(user.Role))
                throw new ApiForbiddenException("This account cannot access the portal.", "PORTAL_ACCESS_DENIED");

            return new PortalAccountRequest
            {
                Name = user.Name,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task UpdateAccountAsync(Guid userId, PortalAccountRequest request, CancellationToken cancellationToken = default)
        {
            UserProfile user = await _db.UserProfiles
                .SingleOrDefaultAsync(profile => profile.Id == userId, cancellationToken)
                ?? throw new ApiNotFoundException("Account not found.", "ACCOUNT_NOT_FOUND");

            if (!IsPortalRole(user.Role))
                throw new ApiForbiddenException("This account cannot access the portal.", "PORTAL_ACCESS_DENIED");

            string name = NormalizeRequired(request.Name, "Name is required.");
            string normalizedPhone = NormalizePhone(request.PhoneNumber);
            bool phoneExists = await _db.UserProfiles
                .AnyAsync(profile => profile.Id != userId && profile.PhoneNumber == normalizedPhone, cancellationToken);

            if (phoneExists)
                throw new AuthConflictException("A user with this phone number already exists.", "PHONE_ALREADY_EXISTS");

            user.Name = name;
            user.PhoneNumber = normalizedPhone;

            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            Manager? manager = await _db.Managers.SingleOrDefaultAsync(existingManager => existingManager.Id == userId, cancellationToken);
            if (manager != null)
                manager.Name = name;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task LogoutAsync(string? sessionId, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
                await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);
        }

        private string NormalizePhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new AuthValidationException("Phone number is required.", "PHONE_REQUIRED");

            try
            {
                return _phoneNumberService.Normalize(phoneNumber);
            }
            catch (ArgumentException ex)
            {
                throw new AuthValidationException(ex.Message, "INVALID_PHONE_NUMBER");
            }
        }

        private static string NormalizeRequired(string? value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new AuthValidationException(message, "VALUE_REQUIRED");

            return value.Trim();
        }

        private async Task<Guid> GetOrCreateAdminMallIdAsync(CancellationToken cancellationToken)
        {
            Guid? existingMallId = await _db.Malls
                .OrderBy(mall => mall.CreatedAt)
                .Select(mall => (Guid?)mall.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingMallId.HasValue)
                return existingMallId.Value;

            var mall = new Mall
            {
                Id = Guid.NewGuid(),
                Name = DefaultAdminMallName,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Malls.Add(mall);
            return mall.Id;
        }

        private static bool IsPortalRole(string role)
            => string.Equals(role, PortalConstants.AdminRole, StringComparison.OrdinalIgnoreCase)
                || role.Contains(PortalConstants.ManagerRole, StringComparison.OrdinalIgnoreCase);
    }
}
