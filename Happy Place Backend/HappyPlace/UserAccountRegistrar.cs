using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System;

namespace HappyWorld.HappyPlace;

public class UserAccountRegistrar {
    // Fields
    private static readonly int VerificationCodeExpirationMinutes = 10;

    // Methods

    public static void RegisterWithEmailAddress(String email, String name, String password) {
        using var dbContext = HappyPlaceDbContext.Create();
        InputValidator.ValidateEmailRegistration(name, email, password, dbContext);
        RemoveExistingPendingEmail(email, dbContext);
        String username = GenerateUsername(email, name, dbContext);
        String verificationCode = CreateEmailUserRecordAndGetVerificationCode(email, name, password, username, dbContext);
        EmailVerificationNotification.Send(email, name, verificationCode);
    }

    public static void RegisterWithPhoneNumber(String phoneNumber, String name, String password) {
        using var dbContext = HappyPlaceDbContext.Create();
        InputValidator.ValidatePhoneRegistration(name, phoneNumber, password, dbContext);
        RemoveExistingPendingPhone(phoneNumber, dbContext);
        String username = GenerateUsername(phoneNumber, name, dbContext);
        String verificationCode = CreatePhoneUserRecordAndGetVerificationCode(phoneNumber, name, password, username, dbContext);
        SmsVerificationNotification.Send(phoneNumber, name, verificationCode);
    }

    public static void ResendEmailVerificationCode(String email) {
        using var dbContext = HappyPlaceDbContext.Create();
        var pendingUserAccount = dbContext.PendingUserAccounts.SingleOrDefault(field => field.EmailAddress == email);
        if (pendingUserAccount == null)
            return;
        Random random = new Random();
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
        Random random = new Random();
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

    public static string GenerateUsername(string emailOrPhone, string name, HappyPlaceDbContext dbContext) {
        Random random = new Random();
        bool validUserName = false;
        int usernameEqualityAttempts = 0;
        int randomNumber;
        string uniqueUsername = "";

        string baseUsername = name.ToLower().Replace(" ", "");
        baseUsername = ResizeBaseUsername(baseUsername, random);

        do {
            randomNumber = random.Next(1, 100);
            uniqueUsername = $"{baseUsername}{randomNumber}";
            validUserName = IsUsernameAvailable(uniqueUsername, dbContext);

            if (!validUserName) ++usernameEqualityAttempts;
            if (usernameEqualityAttempts >= 5) break;
        } while (!validUserName);

        if (usernameEqualityAttempts >= 5) {
            baseUsername = emailOrPhone.Contains("@")
                ? emailOrPhone.ToLower().Split("@")[0]
                : emailOrPhone.ToLower();
            baseUsername = ResizeBaseUsername(baseUsername, random);

            validUserName = false;
            usernameEqualityAttempts = 0;

            do {
                randomNumber = random.Next(1, 100);
                uniqueUsername = $"{baseUsername}{randomNumber}";
                validUserName = IsUsernameAvailable(uniqueUsername, dbContext);

                if (!validUserName) ++usernameEqualityAttempts;
                if (usernameEqualityAttempts >= 5) {
                    throw new Exception("Unable to generate unique username after multiple attempts.");
                }
            } while (!validUserName);
        }
        return uniqueUsername;
    }

    public static String CreateEmailUserRecordAndGetVerificationCode(String email, String name, String password, String username, HappyPlaceDbContext dbContext) {
        String hashedPassword = PasswordHasher.HashPassword(password);
        Random random = new Random();
        String verificationCode = random.Next(100000, 1000000).ToString();
        dbContext.PendingUserAccounts.Add(new PendingUserAccount { EmailAddress = email, DisplayName = name, Username = username, HashedPassword = hashedPassword, VerificationCode = verificationCode, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        return verificationCode;
    }

    public static String CreatePhoneUserRecordAndGetVerificationCode(String phoneNumber, String name, String password, String username, HappyPlaceDbContext dbContext) {
        String hashedPassword = PasswordHasher.HashPassword(password);
        Random random = new Random();
        String verificationCode = random.Next(100000, 1000000).ToString();
        dbContext.PendingUserAccounts.Add(new PendingUserAccount { PhoneNumber = phoneNumber, DisplayName = name, Username = username, HashedPassword = hashedPassword, VerificationCode = verificationCode, CreatedAtUtc = DateTime.UtcNow });
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

    private static bool IsUsernameAvailable(string username, HappyPlaceDbContext dbContext) {
        bool existsInPending = dbContext.PendingUserAccounts.Any(field => field.Username == username);
        bool existsInUsers = dbContext.UserAccounts.Any(field => field.Username == username);
        return !existsInPending && !existsInUsers;
    }

    private static string ResizeBaseUsername(string baseUsername, Random random) {
        string newBaseUsername = baseUsername;
        if (newBaseUsername.Length >= 18) {
            newBaseUsername = newBaseUsername.Substring(0, 18);
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
