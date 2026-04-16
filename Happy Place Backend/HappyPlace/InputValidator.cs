using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public class InputValidator {
    // Methods

    public static void ValidateEmailRegistration(string name, string email, string password, HappyPlaceDbContext dbContext) {
        List<string> validationErrors = new List<string>();

        ValidateName(name, validationErrors);
        ValidateEmailAddress(email, validationErrors);
        ValidatePassword(password, validationErrors);

        if (validationErrors.Count > 0)
            throw new ValidationErrorsException(validationErrors);

        if (dbContext.PendingUserAccounts.Any(field => field.EmailAddress == email))
            validationErrors.Add("An account with this email address already exists.");

        if (validationErrors.Count > 0)
            throw new ValidationErrorsException(validationErrors);
    }

    public static void ValidatePhoneRegistration(string name, string phoneNumber, string password, HappyPlaceDbContext dbContext) {
        List<string> validationErrors = new List<string>();

        ValidateName(name, validationErrors);
        ValidatePhoneNumber(phoneNumber, validationErrors);
        ValidatePassword(password, validationErrors);

        if (validationErrors.Count > 0)
            throw new ValidationErrorsException(validationErrors);

        if (dbContext.PendingUserAccounts.Any(field => field.PhoneNumber == phoneNumber))
            validationErrors.Add("An account with this phone number already exists.");

        if (validationErrors.Count > 0)
            throw new ValidationErrorsException(validationErrors);
    }

    private static void ValidateName(string name, List<string> validationErrors) {
        if (string.IsNullOrWhiteSpace(name)) {
            validationErrors.Add("Name is required.");
            return;
        }

        if (name.Length > 200)
            validationErrors.Add("Name must be 200 characters or less.");
    }

    private static void ValidateEmailAddress(string email, List<string> validationErrors) {
        if (string.IsNullOrWhiteSpace(email)) {
            validationErrors.Add("Email address is required.");
            return;
        }

        if (email.Length > 255) {
            validationErrors.Add("Email address must be 255 characters or less.");
            return;
        }

        if (email.Contains(' ')) {
            validationErrors.Add("Please enter a valid email address.");
            return;
        }

        int firstAtIndex = email.IndexOf('@');
        int lastAtIndex = email.LastIndexOf('@');

        if (firstAtIndex <= 0 || firstAtIndex == email.Length - 1 || firstAtIndex != lastAtIndex) {
            validationErrors.Add("Please enter a valid email address.");
            return;
        }

        string domain = email.Substring(firstAtIndex + 1);
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.'))
            validationErrors.Add("Please enter a valid email address.");
    }

    private static void ValidatePhoneNumber(string phoneNumber, List<string> validationErrors) {
        if (string.IsNullOrWhiteSpace(phoneNumber)) {
            validationErrors.Add("Phone number is required.");
            return;
        }

        if (!phoneNumber.All(char.IsDigit)) {
            validationErrors.Add("Phone number must contain only digits.");
            return;
        }

        if (phoneNumber.Length < 7)
            validationErrors.Add("Phone number is too short.");

        if (phoneNumber.Length > 20)
            validationErrors.Add("Phone number is too long.");
    }

    private static void ValidatePassword(string password, List<string> validationErrors) {
        if (string.IsNullOrEmpty(password)) {
            validationErrors.Add("Password is required.");
            return;
        }

        if (password.Length < 8)
            validationErrors.Add("Password must be at least 8 characters.");

        if (!password.Any(char.IsUpper))
            validationErrors.Add("Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            validationErrors.Add("Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            validationErrors.Add("Password must contain at least one number.");

        if (password.All(char.IsLetterOrDigit))
            validationErrors.Add("Password must contain at least one special character.");
    }
}