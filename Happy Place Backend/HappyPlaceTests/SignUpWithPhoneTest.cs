using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SignUpWithPhoneTest {
    // Tests - Happy Path

    [Fact]
    public void ValidPhoneAccountCredentials() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
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
    public void EmptyNameReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyNameReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "   ",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NameAtExactly200CharactersSucceeds() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = new string('A', 200),
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void NameExceedingMaxLengthReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
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
    public void EmptyPhoneNumberReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = "",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithLettersReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = "949abc5148",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithPlusSignReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = "+19497359148",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithDashesReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = "949-735-9148",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberTooShortReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = "12345",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberAtExactly7DigitsSucceeds() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(7).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberAtExactly20DigitsSucceeds() {
        string guidDigits = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        string uniquePhone = guidDigits + "0123456789";
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberExceedingMaxLengthReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = "123456789012345678901",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Re-Signup and Duplicate Prevention

    [Fact]
    public void ReSignUpWithSamePhoneBeforeVerifyingSucceeds() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage secondAttempt = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.OK, secondAttempt.StatusCode);
    }

    [Fact]
    public void OldVerificationCodeInvalidAfterReSignUp() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        SmsMessage firstSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string oldVerificationCode = SmsVerificationNotification.ExtractVerificationCode(firstSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = oldVerificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void NewVerificationCodeWorksAfterReSignUp() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage secondSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newVerificationCode = SmsVerificationNotification.ExtractVerificationCode(secondSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newVerificationCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("authToken").GetString());
    }

    [Fact]
    public void SignUpWithAlreadyVerifiedPhoneReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage secondSignUp = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, secondSignUp.StatusCode);
    }

    // Tests - Password Validation

    [Fact]
    public void EmptyPasswordReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = ""
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyPasswordReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "        "
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordAtExactly8CharactersMeetingAllRequirementsSucceeds() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven7!a"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PasswordUnderEightCharactersReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Se7en!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingUppercaseReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingLowercaseReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "SEVEN74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingNumberReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Sevennn!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingSpecialCharacterReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven741"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Verification Code Format

    [Fact]
    public void WrongVerificationCodeReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
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
    public void EmptyVerificationCodeReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void PartialVerificationCodeReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithLettersReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "abcdef" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationWithNonExistentPhoneReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    // Tests - Verification Replay Attack

    [Fact]
    public void VerificationCodeReusedAfterSuccessReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();
        HttpResponseMessage secondVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, secondVerify.StatusCode);
    }

    // Tests - Verification Expiration

    [Fact]
    public void ExpiredVerificationCodeReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt9Minutes59SecondsSucceeds() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9).AddSeconds(-59);
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt10Minutes1SecondReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10).AddSeconds(-1);
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithCorruptedCreatedAtReturnsBadRequest() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();

        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pendingAccount.CreatedAtUtc = default;
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }
}
