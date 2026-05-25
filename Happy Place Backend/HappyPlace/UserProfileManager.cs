using HappyWorld.HappyPlace.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;

namespace HappyWorld.HappyPlace;

public class UserProfileManager {
    // Fields

    private static readonly int MinUsernameLength = 5;
    private static readonly int MaxUsernameLength = 20;
    private static readonly int MaxDisplayNameLength = 200;
    private static readonly int MinPasswordLength = 8;
    private static readonly byte ProfilePhotoType = 1;
    private static readonly byte BackgroundPhotoType = 2;
    private static readonly int ProfilePhotoOutputWidth = 400;
    private static readonly int ProfilePhotoOutputHeight = 400;
    private static readonly int BackgroundPhotoOutputWidth = 1200;
    private static readonly int BackgroundPhotoOutputHeight = 400;
    private static readonly int MinPhotoDimensionPixels = 100;
    private static readonly int MaxPhotoDimensionPixels = 8000;
    private static readonly int JpegQuality = 85;
    private static readonly string PhotoContentType = "image/jpeg";
    private static readonly string PhotoUrlPrefix = "/api/photo/";

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
        var authenticatedUser = GetAuthenticatedUserAccount(authToken)
            ?? throw new ValidationErrorsException(["Unable to change password."]);

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
        var authenticatedUser = GetAuthenticatedUserAccount(authToken)
            ?? throw new ValidationErrorsException(["Unable to verify account."]);

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

    // Methods - Photo Upload

    public static MyProfileResult UploadProfilePhoto(string authToken, byte[] photoBytes) {
        return UploadPhoto(authToken, photoBytes, ProfilePhotoType, ProfilePhotoOutputWidth, ProfilePhotoOutputHeight);
    }

    public static MyProfileResult UploadBackgroundPhoto(string authToken, byte[] photoBytes) {
        return UploadPhoto(authToken, photoBytes, BackgroundPhotoType, BackgroundPhotoOutputWidth, BackgroundPhotoOutputHeight);
    }

    // Methods - Photo Remove

    public static MyProfileResult RemoveProfilePhoto(string authToken) {
        return RemovePhoto(authToken, ProfilePhotoType);
    }

    public static MyProfileResult RemoveBackgroundPhoto(string authToken) {
        return RemovePhoto(authToken, BackgroundPhotoType);
    }

    // Methods - Photo Retrieve

    public static UserProfilePhoto GetPhoto(Guid photoId) {
        if (photoId == Guid.Empty)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserProfilePhotos.SingleOrDefault(field => field.Id == photoId);
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

    private static MyProfileResult UploadPhoto(string authToken, byte[] photoBytes, byte photoType, int outputWidth, int outputHeight) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            return null;

        if (photoBytes == null)
            throw new ValidationErrorsException(["Photo is required."]);
        if (photoBytes.Length == 0)
            throw new ValidationErrorsException(["Photo cannot be empty."]);

        if (!HasAcceptedMagicBytes(photoBytes))
            throw new ValidationErrorsException(["Unsupported image format."]);

        ImageInfo imageInfo;
        try {
            imageInfo = Image.Identify(photoBytes);
        }
        catch {
            throw new ValidationErrorsException(["Invalid image file."]);
        }
        if (imageInfo == null)
            throw new ValidationErrorsException(["Invalid image file."]);
        if (imageInfo.Width < MinPhotoDimensionPixels || imageInfo.Height < MinPhotoDimensionPixels)
            throw new ValidationErrorsException(["Image dimensions are too small."]);
        if (imageInfo.Width > MaxPhotoDimensionPixels || imageInfo.Height > MaxPhotoDimensionPixels)
            throw new ValidationErrorsException(["Image dimensions are too large."]);

        byte[] processedBytes;
        try {
            processedBytes = ProcessImage(photoBytes, outputWidth, outputHeight);
        }
        catch {
            throw new ValidationErrorsException(["Invalid image file."]);
        }

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.Id == authenticatedUser.Id);

        var existingPhoto = dbContext.UserProfilePhotos.SingleOrDefault(field => field.UserAccountId == user.Id && field.PhotoType == photoType);
        if (existingPhoto != null)
            dbContext.UserProfilePhotos.Remove(existingPhoto);

        Guid newPhotoId = Guid.NewGuid();
        var newPhoto = new UserProfilePhoto {
            Id = newPhotoId,
            UserAccountId = user.Id,
            PhotoType = photoType,
            ImageBytes = processedBytes,
            ContentType = PhotoContentType,
            FileSizeBytes = processedBytes.Length,
            WidthPixels = outputWidth,
            HeightPixels = outputHeight,
            UploadedAtUtc = DateTime.UtcNow
        };
        dbContext.UserProfilePhotos.Add(newPhoto);

        string photoUrl = $"{PhotoUrlPrefix}{newPhotoId}";
        if (photoType == ProfilePhotoType)
            user.ProfilePhotoUrl = photoUrl;
        else
            user.BackgroundPhotoUrl = photoUrl;

        dbContext.SaveChanges();

        return MyProfileResult.FromUserAccount(user);
    }

    private static MyProfileResult RemovePhoto(string authToken, byte photoType) {
        var authenticatedUser = GetAuthenticatedUserAccount(authToken);
        if (authenticatedUser == null)
            return null;

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.Id == authenticatedUser.Id);

        var existingPhoto = dbContext.UserProfilePhotos.SingleOrDefault(field => field.UserAccountId == user.Id && field.PhotoType == photoType);
        if (existingPhoto != null)
            dbContext.UserProfilePhotos.Remove(existingPhoto);

        if (photoType == ProfilePhotoType)
            user.ProfilePhotoUrl = null;
        else
            user.BackgroundPhotoUrl = null;

        dbContext.SaveChanges();

        return MyProfileResult.FromUserAccount(user);
    }

    private static bool HasAcceptedMagicBytes(byte[] bytes) {
        if (bytes == null || bytes.Length < 12)
            return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return true;
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return true;
        return false;
    }

    private static byte[] ProcessImage(byte[] originalBytes, int outputWidth, int outputHeight) {
        using var image = Image.Load<Rgba32>(originalBytes);
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IccProfile = null;
        image.Mutate(x => x.BackgroundColor(Color.White));
        image.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(outputWidth, outputHeight),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));
        using var outputStream = new MemoryStream();
        image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = JpegQuality });
        return outputStream.ToArray();
    }
}
