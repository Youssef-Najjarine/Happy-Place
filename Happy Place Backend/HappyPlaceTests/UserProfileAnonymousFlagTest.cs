namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class UserProfileAnonymousFlagTest {
    // Tests

    [Fact]
    public void ValidateTokenReportsAGuestAsAnonymous() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = TestUserFactory.CreateGuestUser(container);

        Assert.True(IsAnonymousFromValidateToken(container, guestToken));
    }

    [Fact]
    public void ValidateTokenReportsAVerifiedUserAsNotAnonymous() {
        using var container = new TestingMockProvidersContainer();
        string userToken = TestUserFactory.CreateVerifiedEmailUser(container, "Real Person");

        Assert.False(IsAnonymousFromValidateToken(container, userToken));
    }

    [Fact]
    public void AnUpgradedGuestIsNoLongerReportedAsAnonymous() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = TestUserFactory.CreateGuestUser(container);
        string email = $"user{Guid.NewGuid():N}@gmail.com";
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { AuthToken = guestToken, Name = "Casey", Email = email, Password = "Seven74!" }).EnsureSuccessStatusCode();
        var verificationEmail = container.EmailProvider.EmailMessages.Last();
        string code = HappyWorld.HappyPlace.Email.EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        string accountToken = container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = email, VerificationCode = code })
            .ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        Assert.False(IsAnonymousFromValidateToken(container, accountToken));
    }

    // Helpers

    private static bool IsAnonymousFromValidateToken(TestingMockProvidersContainer container, string authToken) {
        return container.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken })
            .ReadContentAsJsonDocument().RootElement.GetProperty("isAnonymous").GetBoolean();
    }
}
