using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;

namespace HappyWorld.HappyPlace;

public class UserAccountRegistrar {
    // Fields
    private static readonly int VerificationCodeExpirationMinutes = 10;
    private static readonly int MaxEmailLength = 255;
    private static readonly int MaxPhoneLength = 20;
    private static readonly int MaxPasswordLength = 1000;
    private static readonly int ResetTokenByteLength = 32;

    // Methods

    public static void RegisterWithEmailAddress(String email, String name, String password) {
        using var dbContext = HappyPlaceDbContext.Create();
        InputValidator.ValidateEmailRegistration(name, email, password, dbContext);
        RemoveExistingPendingEmail(email, dbContext);
        String username = GenerateUsername(email, name, dbContext);
        try {
            String verificationCode = CreateEmailUserRecordAndGetVerificationCode(email, name, password, username, dbContext);
            EmailVerificationNotification.Send(email, name, verificationCode);
        }
        catch (DbUpdateException) {
            throw new ValidationErrorsException(["Unable to create account. Please try again."]);
        }
    }

    public static void RegisterWithPhoneNumber(String phoneNumber, String name, String password) {
        using var dbContext = HappyPlaceDbContext.Create();
        InputValidator.ValidatePhoneRegistration(name, phoneNumber, password, dbContext);
        RemoveExistingPendingPhone(phoneNumber, dbContext);
        String username = GenerateUsername(phoneNumber, name, dbContext);
        try {
            String verificationCode = CreatePhoneUserRecordAndGetVerificationCode(phoneNumber, name, password, username, dbContext);
            SmsVerificationNotification.Send(phoneNumber, name, verificationCode);
        }
        catch (DbUpdateException) {
            throw new ValidationErrorsException(["Unable to create account. Please try again."]);
        }
    }

    public static SignInResult SignInWithEmailAddress(String email, String password) {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        string trimmedEmail = email.Trim();
        string trimmedPassword = password;

        if (trimmedEmail.Length > MaxEmailLength || trimmedPassword.Length > MaxPasswordLength)
            return null;

        using var dbContext = HappyPlaceDbContext.Create();

        var verifiedAccount = dbContext.UserAccounts.SingleOrDefault(field => field.EmailAddress == trimmedEmail);
        if (verifiedAccount != null) {
            if (!PasswordHasher.VerifyPassword(trimmedPassword, verifiedAccount.HashedPassword))
                return null;
            UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(verifiedAccount.Id.ToString());
            return SignInResult.AsVerified(authToken.ToAuthTokenString());
        }

        var pendingAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.EmailAddress == trimmedEmail);
        if (pendingAccount != null) {
            if (!PasswordHasher.VerifyPassword(trimmedPassword, pendingAccount.HashedPassword))
                return null;
            ResendEmailVerificationCode(trimmedEmail);
            return SignInResult.AsPending(trimmedEmail, "email");
        }

        return null;
    }

    public static SignInResult SignInWithPhoneNumber(String phoneNumber, String password) {
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
            return null;

        string trimmedPhone = phoneNumber.Trim();
        string trimmedPassword = password;

        if (trimmedPhone.Length > MaxPhoneLength || trimmedPassword.Length > MaxPasswordLength)
            return null;

        using var dbContext = HappyPlaceDbContext.Create();

        var verifiedAccount = dbContext.UserAccounts.SingleOrDefault(field => field.PhoneNumber == trimmedPhone);
        if (verifiedAccount != null) {
            if (!PasswordHasher.VerifyPassword(trimmedPassword, verifiedAccount.HashedPassword))
                return null;
            UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(verifiedAccount.Id.ToString());
            return SignInResult.AsVerified(authToken.ToAuthTokenString());
        }

        var pendingAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.PhoneNumber == trimmedPhone);
        if (pendingAccount != null) {
            if (!PasswordHasher.VerifyPassword(trimmedPassword, pendingAccount.HashedPassword))
                return null;
            ResendPhoneVerificationCode(trimmedPhone);
            return SignInResult.AsPending(trimmedPhone, "phone");
        }

        return null;
    }

    public static void ResendEmailVerificationCode(String email) {
        using var dbContext = HappyPlaceDbContext.Create();
        var pendingUserAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.EmailAddress == email);
        if (pendingUserAccount == null)
            return;
        Random random = new();
        String verificationCode = random.Next(100000, 1000000).ToString();
        pendingUserAccount.VerificationCode = verificationCode;
        pendingUserAccount.CreatedAtUtc = DateTime.UtcNow;
        dbContext.SaveChanges();
        EmailVerificationNotification.Send(email, pendingUserAccount.DisplayName, verificationCode);
    }

    public static void ResendPhoneVerificationCode(String phoneNumber) {
        using var dbContext = HappyPlaceDbContext.Create();
        var pendingUserAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.PhoneNumber == phoneNumber);
        if (pendingUserAccount == null)
            return;
        Random random = new();
        String verificationCode = random.Next(100000, 1000000).ToString();
        pendingUserAccount.VerificationCode = verificationCode;
        pendingUserAccount.CreatedAtUtc = DateTime.UtcNow;
        dbContext.SaveChanges();
        SmsVerificationNotification.Send(phoneNumber, pendingUserAccount.DisplayName, verificationCode);
    }

    public static UserAccount VerifyEmailAddress(String email, String verificationCode) {
        using var dbContext = HappyPlaceDbContext.Create();
        var pendingUserAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.EmailAddress == email && field.VerificationCode == verificationCode);
        if (pendingUserAccount == null)
            return null;
        if (DateTime.UtcNow - pendingUserAccount.CreatedAtUtc > TimeSpan.FromMinutes(VerificationCodeExpirationMinutes))
            return null;
        UserAccount userAccount = CreateUserAccountFromPending(pendingUserAccount, dbContext);
        EmailAccountVerifiedNotification.Send(email, userAccount.DisplayName);
        return userAccount;
    }

    public static UserAccount VerifyPhoneNumber(String phoneNumber, String verificationCode) {
        using var dbContext = HappyPlaceDbContext.Create();
        var pendingUserAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.PhoneNumber == phoneNumber && field.VerificationCode == verificationCode);
        if (pendingUserAccount == null)
            return null;
        if (DateTime.UtcNow - pendingUserAccount.CreatedAtUtc > TimeSpan.FromMinutes(VerificationCodeExpirationMinutes))
            return null;
        UserAccount userAccount = CreateUserAccountFromPending(pendingUserAccount, dbContext);
        SmsAccountVerifiedNotification.Send(phoneNumber, userAccount.DisplayName);
        return userAccount;
    }

    public static void RequestPasswordResetWithEmail(String email) {
        string trimmedEmail = (email ?? "").Trim();
        InputValidator.ValidateForgotPasswordEmail(trimmedEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var verifiedAccount = dbContext.UserAccounts.SingleOrDefault(field => field.EmailAddress == trimmedEmail);
        if (verifiedAccount == null)
            return;

        string canonicalEmail = verifiedAccount.EmailAddress;
        RemoveExistingPasswordResetRequestForEmail(canonicalEmail, dbContext);
        String verificationCode = CreatePasswordResetRequestForEmailAndGetCode(canonicalEmail, dbContext);
        ForgotPasswordEmailNotification.Send(canonicalEmail, verifiedAccount.DisplayName, verificationCode);
    }

    public static void RequestPasswordResetWithPhone(String phoneNumber) {
        string trimmedPhone = (phoneNumber ?? "").Trim();
        InputValidator.ValidateForgotPasswordPhone(trimmedPhone);

        using var dbContext = HappyPlaceDbContext.Create();
        var verifiedAccount = dbContext.UserAccounts.SingleOrDefault(field => field.PhoneNumber == trimmedPhone);
        if (verifiedAccount == null)
            return;

        string canonicalPhone = verifiedAccount.PhoneNumber;
        RemoveExistingPasswordResetRequestForPhone(canonicalPhone, dbContext);
        String verificationCode = CreatePasswordResetRequestForPhoneAndGetCode(canonicalPhone, dbContext);
        ForgotPasswordSmsNotification.Send(canonicalPhone, verifiedAccount.DisplayName, verificationCode);
    }

    public static String VerifyForgotPasswordEmail(String email, String verificationCode) {
        string trimmedEmail = (email ?? "").Trim();
        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(
            field => field.EmailAddress == trimmedEmail && field.VerificationCode == verificationCode);

        if (resetRequest == null)
            return null;
        if (resetRequest.VerifiedAt != null)
            return null;
        if (DateTime.UtcNow > resetRequest.ExpiresAt)
            return null;

        String resetToken = GenerateResetToken();
        resetRequest.ResetToken = resetToken;
        resetRequest.VerifiedAt = DateTime.UtcNow;
        resetRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(VerificationCodeExpirationMinutes);
        dbContext.SaveChanges();

        return resetToken;
    }

    public static String VerifyForgotPasswordPhone(String phoneNumber, String verificationCode) {
        string trimmedPhone = (phoneNumber ?? "").Trim();
        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(
            field => field.PhoneNumber == trimmedPhone && field.VerificationCode == verificationCode);

        if (resetRequest == null)
            return null;
        if (resetRequest.VerifiedAt != null)
            return null;
        if (DateTime.UtcNow > resetRequest.ExpiresAt)
            return null;

        String resetToken = GenerateResetToken();
        resetRequest.ResetToken = resetToken;
        resetRequest.VerifiedAt = DateTime.UtcNow;
        resetRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(VerificationCodeExpirationMinutes);
        dbContext.SaveChanges();

        return resetToken;
    }

    public static void ResetPassword(String resetToken, String newPassword) {
        InputValidator.ValidateResetPassword(resetToken, newPassword);

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(field => field.ResetToken == resetToken);

        if (resetRequest == null)
            throw new ValidationErrorsException(["Invalid or expired reset token."]);
        if (resetRequest.UsedAt != null)
            throw new ValidationErrorsException(["This reset link has already been used."]);
        if (DateTime.UtcNow > resetRequest.ExpiresAt)
            throw new ValidationErrorsException(["This reset link has expired."]);

        UserAccount userAccount = null;
        if (!string.IsNullOrEmpty(resetRequest.EmailAddress))
            userAccount = dbContext.UserAccounts.SingleOrDefault(field => field.EmailAddress == resetRequest.EmailAddress);
        else if (!string.IsNullOrEmpty(resetRequest.PhoneNumber))
            userAccount = dbContext.UserAccounts.SingleOrDefault(field => field.PhoneNumber == resetRequest.PhoneNumber);

        if (userAccount == null)
            throw new ValidationErrorsException(["Account not found."]);

        userAccount.HashedPassword = PasswordHasher.HashPassword(newPassword);
        resetRequest.UsedAt = DateTime.UtcNow;
        dbContext.SaveChanges();
    }

    public static string GenerateUsername(string emailOrPhone, string name, HappyPlaceDbContext dbContext) {
        Random random = new();
        int usernameEqualityAttempts = 0;
        string uniqueUsername;

        string baseUsername = name.ToLower().Replace(" ", "");
        baseUsername = ResizeBaseUsername(baseUsername, random);

        do {
            int randomNumber = random.Next(1, 100);
            uniqueUsername = $"{baseUsername}{randomNumber}";
            bool validUserName = IsUsernameAvailable(uniqueUsername, dbContext);

            if (!validUserName) ++usernameEqualityAttempts;
            else break;
            if (usernameEqualityAttempts >= 5) break;
        } while (true);

        if (usernameEqualityAttempts >= 5) {
            baseUsername = emailOrPhone.Contains('@')
                ? emailOrPhone.ToLower().Split('@')[0]
                : emailOrPhone.ToLower();
            baseUsername = ResizeBaseUsername(baseUsername, random);

            usernameEqualityAttempts = 0;

            do {
                int randomNumber = random.Next(1, 100);
                uniqueUsername = $"{baseUsername}{randomNumber}";
                bool validUserName = IsUsernameAvailable(uniqueUsername, dbContext);

                if (!validUserName) ++usernameEqualityAttempts;
                else break;
                if (usernameEqualityAttempts >= 5) {
                    throw new Exception("Unable to generate unique username after multiple attempts.");
                }
            } while (true);
        }
        return uniqueUsername;
    }

    public static String CreateEmailUserRecordAndGetVerificationCode(String email, String name, String password, String username, HappyPlaceDbContext dbContext) {
        String hashedPassword = PasswordHasher.HashPassword(password);
        Random random = new();
        String verificationCode = random.Next(100000, 1000000).ToString();
        dbContext.PendingUserAccounts.Add(new() { EmailAddress = email, DisplayName = name, Username = username, HashedPassword = hashedPassword, VerificationCode = verificationCode, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        return verificationCode;
    }

    public static String CreatePhoneUserRecordAndGetVerificationCode(String phoneNumber, String name, String password, String username, HappyPlaceDbContext dbContext) {
        String hashedPassword = PasswordHasher.HashPassword(password);
        Random random = new();
        String verificationCode = random.Next(100000, 1000000).ToString();
        dbContext.PendingUserAccounts.Add(new() { PhoneNumber = phoneNumber, DisplayName = name, Username = username, HashedPassword = hashedPassword, VerificationCode = verificationCode, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        return verificationCode;
    }

    private static UserAccount CreateUserAccountFromPending(PendingUserAccount pendingUserAccount, HappyPlaceDbContext dbContext) {
        var userAccount = new UserAccount {
            Username = pendingUserAccount.Username,
            HashedPassword = pendingUserAccount.HashedPassword,
            DisplayName = pendingUserAccount.DisplayName,
            EmailAddress = pendingUserAccount.EmailAddress,
            PhoneNumber = pendingUserAccount.PhoneNumber,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.UserAccounts.Add(userAccount);
        dbContext.PendingUserAccounts.Remove(pendingUserAccount);
        dbContext.SaveChanges();
        return userAccount;
    }

    private static void RemoveExistingPendingEmail(String email, HappyPlaceDbContext dbContext) {
        var existingPending = dbContext.PendingUserAccounts.SingleOrDefault(field => field.EmailAddress == email);
        if (existingPending != null) {
            dbContext.PendingUserAccounts.Remove(existingPending);
            dbContext.SaveChanges();
        }
    }

    private static void RemoveExistingPendingPhone(String phoneNumber, HappyPlaceDbContext dbContext) {
        var existingPending = dbContext.PendingUserAccounts.SingleOrDefault(field => field.PhoneNumber == phoneNumber);
        if (existingPending != null) {
            dbContext.PendingUserAccounts.Remove(existingPending);
            dbContext.SaveChanges();
        }
    }

    private static String CreatePasswordResetRequestForEmailAndGetCode(String email, HappyPlaceDbContext dbContext) {
        Random random = new();
        String verificationCode = random.Next(100000, 1000000).ToString();
        DateTime now = DateTime.UtcNow;
        var resetRequest = new PasswordResetRequest {
            EmailAddress = email,
            VerificationCode = verificationCode,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(VerificationCodeExpirationMinutes)
        };
        dbContext.PasswordResetRequests.Add(resetRequest);
        dbContext.SaveChanges();
        return verificationCode;
    }

    private static String CreatePasswordResetRequestForPhoneAndGetCode(String phoneNumber, HappyPlaceDbContext dbContext) {
        Random random = new();
        String verificationCode = random.Next(100000, 1000000).ToString();
        DateTime now = DateTime.UtcNow;
        var resetRequest = new PasswordResetRequest {
            PhoneNumber = phoneNumber,
            VerificationCode = verificationCode,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(VerificationCodeExpirationMinutes)
        };
        dbContext.PasswordResetRequests.Add(resetRequest);
        dbContext.SaveChanges();
        return verificationCode;
    }

    private static void RemoveExistingPasswordResetRequestForEmail(String email, HappyPlaceDbContext dbContext) {
        var existingRequests = dbContext.PasswordResetRequests.Where(field => field.EmailAddress == email).ToList();
        if (existingRequests.Count > 0) {
            dbContext.PasswordResetRequests.RemoveRange(existingRequests);
            dbContext.SaveChanges();
        }
    }

    private static void RemoveExistingPasswordResetRequestForPhone(String phoneNumber, HappyPlaceDbContext dbContext) {
        var existingRequests = dbContext.PasswordResetRequests.Where(field => field.PhoneNumber == phoneNumber).ToList();
        if (existingRequests.Count > 0) {
            dbContext.PasswordResetRequests.RemoveRange(existingRequests);
            dbContext.SaveChanges();
        }
    }

    private static String GenerateResetToken() {
        byte[] randomBytes = new byte[ResetTokenByteLength];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static bool IsUsernameAvailable(string username, HappyPlaceDbContext dbContext) {
        bool existsInPending = dbContext.PendingUserAccounts.Any(field => field.Username == username);
        bool existsInUsers = dbContext.UserAccounts.Any(field => field.Username == username);
        return !existsInPending && !existsInUsers;
    }

    private static string ResizeBaseUsername(string baseUsername, Random random) {
        string newBaseUsername = baseUsername;
        if (newBaseUsername.Length >= 18) {
            newBaseUsername = newBaseUsername[..18];
        }
        else if (newBaseUsername.Length <= 3) {
            newBaseUsername = GenerateMinimumBaseUsername(newBaseUsername, random);
        }
        return newBaseUsername;
    }

    private static string GenerateMinimumBaseUsername(string baseUsername, Random random) {
        string newBaseUsername = baseUsername;
        while (newBaseUsername.Length < 4) {
            int randomAlphabetNumber = random.Next(0, 26);
            char randomLetter = (char)('a' + randomAlphabetNumber);
            newBaseUsername += randomLetter;
        }
        return newBaseUsername;
    }
}
