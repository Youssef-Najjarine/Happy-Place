using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SignUpWithPhoneTest
{
    // Tests - Happy Path

    [Fact]
    public void ValidPhoneAccountCredentials()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("authToken").GetString());
    }

    // Tests - Name Validation

    [Fact]
    public void EmptyNameReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyNameReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "   ",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NameExceedingMaxLengthReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = new string('A', 201),
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Phone Number Validation

    [Fact]
    public void EmptyPhoneNumberReturnsBadRequest()
    {
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = "",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithLettersReturnsBadRequest()
    {
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = "949abc5148",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberTooShortReturnsBadRequest()
    {
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = "12345",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberExceedingMaxLengthReturnsBadRequest()
    {
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = "123456789012345678901",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void DuplicatePhoneNumberReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage secondAttempt = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    // Tests - Password Validation

    [Fact]
    public void EmptyPasswordReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = ""
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordUnderEightCharactersReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Se7en!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingUppercaseReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingLowercaseReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "SEVEN74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingNumberReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Sevennn!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingSpecialCharacterReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven741"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Verification

    [Fact]
    public void WrongVerificationCodeReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new
        {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationWithNonExistentPhoneReturnsBadRequest()
    {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }
}