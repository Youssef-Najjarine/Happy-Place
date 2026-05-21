using HappyWorld.HappyPlace.Data;
using System.Text.RegularExpressions;

namespace HappyWorld.HappyPlace;

public class UserProfileManager {
    // Fields

    private static readonly int MinUsernameLength = 5;
    private static readonly int MaxUsernameLength = 20;
    private static readonly int MaxDisplayNameLength = 200;
    private static readonly int MinPasswordLength = 8;

    // Methods - Profile Retrieval

    public static MyProfileResult GetMyProfile(string authToken) {
        var userAccount = GetAuthenticatedUserAccount(authToken);
        if (userAccount == null)
            return null;
        return MyProfileResult.FromUserAccount(userAccount);
    }

    public static bool IsAuthenticated(string authToken) {
        return GetAuthenticatedUserAccount(authToken) != null;
    }

    public static PublicProfileResult GetPublicProfile(string username) {
        if (string.IsNullOrWhiteSpace(username))
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.SingleOrDefault(field => field.Username == username);
        if (userAccount == null)
            return null;
        return PublicProfileResult.FromUserAccount(userAccount);
    }

    // Methods - Profile Update

    public static MyProfileResult UpdateProfile(string authToken, string username, string displayName, string bio) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            return null;

        string normalizedUsername = (username ?? "").Trim().ToLowerInvariant();
        ValidateUsernameFormat(normalizedUsername);

        string trimmedName = (displayName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new ValidationErrorsException(["Display name is required."]);
        if (trimmedName.Length > MaxDisplayNameLength)
            throw new ValidationErrorsException([$"Display name must be {MaxDisplayNameLength} characters or less."]);

        using var dbContext = HappyPlaceDbContext.Create();

        if (normalizedUsername != authenticatedUser.Username) {
            bool usernameTaken = dbContext.UserAccounts.Any(field => field.Username == normalizedUsername);
            bool pendingTaken = dbContext.PendingUserAccounts.Any(field => field.Username == normalizedUsername);
            if (usernameTaken || pendingTaken)
                throw new ValidationErrorsException(["This username is already taken."]);
        }

        var user = dbContext.UserAccounts.Single(field => field.Id == authenticatedUser.Id);
        user.Username = normalizedUsername;
        user.DisplayName = trimmedName;
        user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
        dbContext.SaveChanges();

        return MyProfileResult.FromUserAccount(user);
    }

    // Methods - Password

    public static void ChangePassword(string authToken, string currentPassword, string newPassword) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            throw new ValidationErrorsException(["Unable to change password."]);

        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new ValidationErrorsException(["Current password is required."]);

        if (!PasswordHasher.VerifyPassword(currentPassword, authenticatedUser.HashedPassword))
            throw new ValidationErrorsException(["Current password is incorrect."]);

        ValidateNewPassword(newPassword);

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.Id == authenticatedUser.Id);
        user.HashedPassword = PasswordHasher.HashPassword(newPassword);
        dbContext.SaveChanges();
    }

    public static PasswordVerificationResult VerifyCurrentPassword(string authToken, string password) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            return new PasswordVerificationResult(false);

        if (string.IsNullOrWhiteSpace(password))
            return new PasswordVerificationResult(false);

        bool isValid = PasswordHasher.VerifyPassword(password, authenticatedUser.HashedPassword);
        return new PasswordVerificationResult(isValid);
    }

    // Methods - Username

    public static UsernameAvailabilityResult CheckUsernameAvailability(string authToken, string username) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            return new UsernameAvailabilityResult(false);

        if (string.IsNullOrWhiteSpace(username))
            return new UsernameAvailabilityResult(false);

        string normalizedUsername = username.Trim().ToLowerInvariant();

        if (normalizedUsername == authenticatedUser.Username)
            return new UsernameAvailabilityResult(true);

        using var dbContext = HappyPlaceDbContext.Create();
        bool takenByUser = dbContext.UserAccounts.Any(field => field.Username == normalizedUsername);
        bool takenByPending = dbContext.PendingUserAccounts.Any(field => field.Username == normalizedUsername);

        return new UsernameAvailabilityResult(!takenByUser && !takenByPending);
    }

    // Methods - Account Deletion

    public static void DeleteAccount(string authToken, string password) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            throw new ValidationErrorsException(["Unable to verify account."]);

        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationErrorsException(["Password is required."]);

        if (!PasswordHasher.VerifyPassword(password, authenticatedUser.HashedPassword))
            throw new ValidationErrorsException(["Password is incorrect."]);

        using var dbContext = HappyPlaceDbContext.Create();

        var passwordResetRequests = dbContext.PasswordResetRequests
            .Where(field => field.EmailAddress == authenticatedUser.EmailAddress || field.PhoneNumber == authenticatedUser.PhoneNumber)
            .ToList();
        if (passwordResetRequests.Count > 0)
            dbContext.PasswordResetRequests.RemoveRange(passwordResetRequests);

        var user = dbContext.UserAccounts.Single(field => field.Id == authenticatedUser.Id);
        dbContext.UserAccounts.Remove(user);
        dbContext.SaveChanges();
    }

    // Methods - Private

    private static UserAccount GetAuthenticatedUserAccount(string authToken) {
        if (string.IsNullOrWhiteSpace(authToken))
            return null;
        UserAuthenticationToken token;
        try {
            token = UserAuthenticationToken.ValidateToken(authToken);
        }
        catch {
            return null;
        }
        if (token == null)
            return null;
        if (!Guid.TryParse(token.Identifier, out Guid userId))
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.SingleOrDefault(field => field.Id == userId);
    }

    private static void ValidateUsernameFormat(string normalizedUsername) {
        if (normalizedUsername.Length < MinUsernameLength)
            throw new ValidationErrorsException([$"Username must be at least {MinUsernameLength} characters."]);
        if (normalizedUsername.Length > MaxUsernameLength)
            throw new ValidationErrorsException([$"Username must be {MaxUsernameLength} characters or less."]);
        if (!Regex.IsMatch(normalizedUsername, @"^[a-z0-9]+$"))
            throw new ValidationErrorsException(["Username can only contain letters and numbers."]);
        if (!Regex.IsMatch(normalizedUsername, @"\d"))
            throw new ValidationErrorsException(["Username must contain at least one number."]);
    }

    private static void ValidateNewPassword(string password) {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ValidationErrorsException([$"Password must be at least {MinPasswordLength} characters."]);
        if (!Regex.IsMatch(password, @"\d"))
            throw new ValidationErrorsException(["Password must contain at least one number."]);
        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new ValidationErrorsException(["Password must contain at least one lowercase letter."]);
        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new ValidationErrorsException(["Password must contain at least one uppercase letter."]);
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9\s]"))
            throw new ValidationErrorsException(["Password must contain at least one special character."]);
    }
}
