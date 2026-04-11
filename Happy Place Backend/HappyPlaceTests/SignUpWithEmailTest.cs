using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

public class SignUpWithEmailTest
{
    [Fact]
    public void ValidAccountCredentials()
    {
        var jsonData = new {
            Name =  "Youssef Najjarine",
            EmailAddress = "ynajjarine@gmail.com",
            DisplayName = "Youssef Najjarine",
            password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { jsonData.EmailAddress, verificationCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("authToken").GetString());
    }
}
