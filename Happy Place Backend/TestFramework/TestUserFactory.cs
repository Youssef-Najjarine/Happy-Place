using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;

namespace HappyWorld.HappyPlace;

public static class TestUserFactory {
    // Methods

    public static string CreateVerifiedEmailUser(TestingMockProvidersContainer testingMockProvidersContainer, string displayName) {
        string uniqueEmail = $"user{Guid.NewGuid():N}@gmail.com";
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = displayName, Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }

    public static string CreateVerifiedPhoneUser(TestingMockProvidersContainer testingMockProvidersContainer, string displayName) {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = displayName, PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }

    public static string CreateGuestUser(TestingMockProvidersContainer testingMockProvidersContainer) {
        HttpResponseMessage createResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/createGuest", new { });
        return createResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }
}
