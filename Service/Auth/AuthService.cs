using Graduation_Project_Backend.Data;
using Graduation_Project_Backend.DTOs;
using Graduation_Project_Backend.DTOs.Auth;
using Graduation_Project_Backend.Models.Entities;
using Graduation_Project_Backend.Models.User;
using Graduation_Project_Backend.Service.Common;
using Graduation_Project_Backend.Service.Session;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project_Backend.Service.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IPhoneNumberService _phoneNumberService;
        private readonly IPasswordHasher<UserProfile> _passwordHasher;
        private readonly ISessionService _sessionService;

        public AuthService(
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

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto? dto, CancellationToken cancellationToken = default)
        {
            RegisterRequestDto request = ValidateRegisterRequest(dto);

            string normalizedPhone = NormalizePhone(request.PhoneNumber);
            UserProfile? existingUser = await _db.UserProfiles
                .FirstOrDefaultAsync(user => user.PhoneNumber == normalizedPhone, cancellationToken);

            if (request.ManagerId.HasValue)
            {
                UserProfile managerUser = await RegisterManagerAsync(request, normalizedPhone, existingUser, cancellationToken);
                UserSession managerSession = await _sessionService.CreateOrReplaceSessionAsync(managerUser.Id, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                return CreateResponse("Manager registered successfully.", managerUser, managerSession.Id);
            }

            if (existingUser != null)
            {
                if (existingUser.MallID == request.MallID)
                    throw new AuthConflictException("A user with this phone number already exists for the selected mall.", "USER_ALREADY_EXISTS");

                throw new AuthConflictException("This phone number is already registered for a different mall.", "PHONE_ALREADY_REGISTERED");
            }

            var user = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                PhoneNumber = normalizedPhone,
                Role = "user",
                TotalPoints = 0,
                MallID = request.MallID
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _db.UserProfiles.Add(user);

            UserSession session = await _sessionService.CreateOrReplaceSessionAsync(user.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return CreateResponse("Registered successfully.", user, session.Id);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto? dto, CancellationToken cancellationToken = default)
        {
            LoginRequestDto request = ValidateLoginRequest(dto);

            string normalizedPhone = NormalizePhone(request.PhoneNumber);
            UserProfile? user = await _db.UserProfiles
                .FirstOrDefaultAsync(
                    existingUser => existingUser.PhoneNumber == normalizedPhone && existingUser.MallID == request.MallID,
                    cancellationToken);

            if (user == null)
                throw new AuthUnauthorizedException("Invalid phone number or password.", "INVALID_CREDENTIALS");

            PasswordVerificationResult verificationResult =
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
                throw new AuthUnauthorizedException("Invalid phone number or password.", "INVALID_CREDENTIALS");

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            UserSession session = await _sessionService.CreateOrReplaceSessionAsync(user.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return CreateResponse("Logged in successfully.", user, session.Id);
        }

        public async Task<AuthResponseDto> ManagerQuickLoginAsync(ManagerQuickLoginRequestDto? dto, CancellationToken cancellationToken = default)
        {
            Guid managerId = ValidateManagerQuickLoginRequest(dto);

            Manager manager = await _db.Managers
                .AsNoTracking()
                .SingleOrDefaultAsync(existingManager => existingManager.Id == managerId, cancellationToken)
                ?? throw new AuthValidationException("Manager ID was not found.", "MANAGER_NOT_FOUND");

            UserProfile? user = await _db.UserProfiles
                .FirstOrDefaultAsync(existingUser => existingUser.Id == managerId, cancellationToken);

            if (user == null)
            {
                user = CreateManagerUserProfile(manager);
                _db.UserProfiles.Add(user);
            }
            else
            {
                user.Name = manager.Name.Trim();
                user.Role = string.IsNullOrWhiteSpace(manager.Role) ? "manager" : manager.Role.Trim();
                user.MallID = manager.MallID;

                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                    user.PhoneNumber = BuildManagerPlaceholderPhone(manager.Id);

                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                    user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));
            }

            UserSession session = await _sessionService.CreateOrReplaceSessionAsync(user.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return CreateResponse("Manager quick login completed.", user, session.Id);
        }

        public async Task LogoutAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new AuthValidationException("SessionId is required.", "SESSION_ID_REQUIRED");

            bool removed = await _sessionService.DeleteSessionAsync(sessionId, cancellationToken);
            if (!removed)
                throw new AuthNotFoundException("Session not found.", "SESSION_NOT_FOUND");
        }

        private RegisterRequestDto ValidateRegisterRequest(RegisterRequestDto? dto)
        {
            if (dto == null)
                throw new AuthValidationException("Request body is required.", "INVALID_BODY");

            if (!dto.ManagerId.HasValue && string.IsNullOrWhiteSpace(dto.Name))
                throw new AuthValidationException("Name is required.", "NAME_REQUIRED");

            if (dto.ManagerId.HasValue && dto.ManagerId.Value == Guid.Empty)
                throw new AuthValidationException("ManagerId is invalid.", "MANAGER_ID_INVALID");

            ValidateCredentials(dto.PhoneNumber, dto.Password, dto.MallID);
            return dto;
        }

        private LoginRequestDto ValidateLoginRequest(LoginRequestDto? dto)
        {
            if (dto == null)
                throw new AuthValidationException("Request body is required.", "INVALID_BODY");

            ValidateCredentials(dto.PhoneNumber, dto.Password, dto.MallID);
            return dto;
        }

        private Guid ValidateManagerQuickLoginRequest(ManagerQuickLoginRequestDto? dto)
        {
            if (dto == null)
                throw new AuthValidationException("Request body is required.", "INVALID_BODY");

            if (dto.ManagerId == Guid.Empty)
                throw new AuthValidationException("ManagerId is invalid.", "MANAGER_ID_INVALID");

            return dto.ManagerId;
        }

        private void ValidateCredentials(string phoneNumber, string password, Guid mallId)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new AuthValidationException("PhoneNumber is required.", "PHONE_NUMBER_REQUIRED");

            if (string.IsNullOrWhiteSpace(password))
                throw new AuthValidationException("Password is required.", "PASSWORD_REQUIRED");

            if (mallId == Guid.Empty)
                throw new AuthValidationException("MallID is required.", "MALL_ID_REQUIRED");
        }

        private string NormalizePhone(string phoneNumber)
        {
            try
            {
                return _phoneNumberService.Normalize(phoneNumber);
            }
            catch (ArgumentException ex)
            {
                throw new AuthValidationException(ex.Message, "INVALID_PHONE_NUMBER");
            }
        }

        private static AuthResponseDto CreateResponse(string message, UserProfile user, string sessionId)
            => new()
            {
                Message = message,
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                TotalPoints = user.TotalPoints,
                Role = user.Role,
                SessionId = sessionId
            };

        private UserProfile CreateManagerUserProfile(Manager manager)
        {
            var user = new UserProfile
            {
                Id = manager.Id,
                Name = manager.Name.Trim(),
                PhoneNumber = BuildManagerPlaceholderPhone(manager.Id),
                Role = string.IsNullOrWhiteSpace(manager.Role) ? "manager" : manager.Role.Trim(),
                TotalPoints = 0,
                MallID = manager.MallID
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));
            return user;
        }

        private static string BuildManagerPlaceholderPhone(Guid managerId)
            => $"manager-{managerId:N}";

        private async Task<UserProfile> RegisterManagerAsync(
            RegisterRequestDto request,
            string normalizedPhone,
            UserProfile? existingUserByPhone,
            CancellationToken cancellationToken)
        {
            Guid managerId = request.ManagerId!.Value;
            Manager manager = await _db.Managers
                .AsNoTracking()
                .SingleOrDefaultAsync(existingManager => existingManager.Id == managerId, cancellationToken)
                ?? throw new AuthValidationException("Manager ID was not found.", "MANAGER_NOT_FOUND");

            if (manager.MallID != request.MallID)
                throw new AuthValidationException("Manager does not belong to the selected mall.", "MANAGER_MALL_MISMATCH");

            UserProfile? existingUserByManagerId = await _db.UserProfiles
                .FirstOrDefaultAsync(user => user.Id == managerId, cancellationToken);

            if (existingUserByManagerId != null)
                throw new AuthConflictException("This manager account is already registered.", "MANAGER_ALREADY_REGISTERED");

            if (existingUserByPhone != null)
            {
                if (existingUserByPhone.MallID == request.MallID)
                    throw new AuthConflictException("A user with this phone number already exists for the selected mall.", "USER_ALREADY_EXISTS");

                throw new AuthConflictException("This phone number is already registered for a different mall.", "PHONE_ALREADY_REGISTERED");
            }

            var user = new UserProfile
            {
                Id = manager.Id,
                Name = manager.Name.Trim(),
                PhoneNumber = normalizedPhone,
                Role = string.IsNullOrWhiteSpace(manager.Role) ? "manager" : manager.Role.Trim(),
                TotalPoints = 0,
                MallID = manager.MallID
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _db.UserProfiles.Add(user);
            return user;
        }
    }
}
