namespace HappyWorld.HappyPlace;

public class UserProfileResult {
    // Properties
    public string UserId { get; private set; }
    public string DisplayName { get; private set; }
    public string Username { get; private set; }
    public string EmailAddress { get; private set; }
    public string PhoneNumber { get; private set; }

    // Methods
    public static UserProfileResult FromUserAccount(Data.UserAccount userAccount) {
        return new() {
            UserId = userAccount.Id.ToString(),
            DisplayName = userAccount.DisplayName,
            Username = userAccount.Username,
            EmailAddress = userAccount.EmailAddress,
            PhoneNumber = userAccount.PhoneNumber
        };
    }
}
