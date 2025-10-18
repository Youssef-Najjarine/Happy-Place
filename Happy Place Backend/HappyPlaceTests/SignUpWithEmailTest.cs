using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading.Tasks;
using WebApp;

namespace HappyWorld.HappyPlace;

public class SignUpWithEmailTest
{
    [Fact]
    public void ValidAccountCredentials()
    {
        var jsonData = new { 
            email = "ynajjarine@gmail.com", 
            name = "Youssef Najjarine",
            password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { jsonData.email, verificationCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("authToken").GetString());

        //var webApplicationFactory = new WebApplicationFactory<Program>();
        //var client = webApplicationFactory.CreateClient();
        //var response = await client.PostAsync("api/authentication/signUp", JsonContent.Create(jsonData));
        //Assert.Equal(expectedStatusCode, response.StatusCode);
    }
}
