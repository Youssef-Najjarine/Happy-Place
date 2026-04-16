using System.Net;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SignUpWithEmailTest {
    // Tests - Happy Path

    [Fact]
    public void ValidAccountCredentials() {
        string uniqueEmail = $"valid{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("authToken").GetString());
    }

    // Tests - Name Validation

    [Fact]
    public void EmptyNameReturnsBadRequest() {
        string uniqueEmail = $"emptyname{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyNameReturnsBadRequest() {
        string uniqueEmail = $"wsname{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "   ",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NameAtExactly200CharactersSucceeds() {
        string uniqueEmail = $"name200{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = new string('A', 200),
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void NameExceedingMaxLengthReturnsBadRequest() {
        string uniqueEmail = $"longname{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = new string('A', 201),
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Email Validation

    [Fact]
    public void EmptyEmailReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingAtSymbolReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "ynajjarinegmail.com",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailWithMultipleAtSignsReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "user@@gmail.com",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailWithSpacesReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "ynajjarine @gmail.com",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingDomainReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "ynajjarine@",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingUsernameReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "@gmail.com",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingDotInDomainReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "ynajjarine@gmailcom",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailDomainStartingWithDotReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "ynajjarine@.gmail.com",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailDomainEndingWithDotReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = "ynajjarine@gmail.com.",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailAtExactly255CharactersSucceeds() {
        string guid = Guid.NewGuid().ToString("N");
        string localPart = guid + new string('a', 245 - guid.Length);
        string uniqueEmail = $"{localPart}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void EmailExceedingMaxLengthReturnsBadRequest() {
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = new string('a', 246) + "@gmail.com",
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void DuplicateEmailReturnsBadRequest() {
        string uniqueEmail = $"dup{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage secondAttempt = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    // Tests - Password Validation

    [Fact]
    public void EmptyPasswordReturnsBadRequest() {
        string uniqueEmail = $"emptypw{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = ""
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyPasswordReturnsBadRequest() {
        string uniqueEmail = $"wspw{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "        "
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordAtExactly8CharactersMeetingAllRequirementsSucceeds() {
        string uniqueEmail = $"pw8{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven7!a"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PasswordUnderEightCharactersReturnsBadRequest() {
        string uniqueEmail = $"shortpw{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Se7en!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingUppercaseReturnsBadRequest() {
        string uniqueEmail = $"nouppr{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingLowercaseReturnsBadRequest() {
        string uniqueEmail = $"nolowr{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "SEVEN74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingNumberReturnsBadRequest() {
        string uniqueEmail = $"nonum{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Sevennn!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingSpecialCharacterReturnsBadRequest() {
        string uniqueEmail = $"nospec{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven741"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Verification Code Format

    [Fact]
    public void WrongVerificationCodeReturnsBadRequest() {
        string uniqueEmail = $"wrongcode{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void EmptyVerificationCodeReturnsBadRequest() {
        string uniqueEmail = $"emptycode{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void PartialVerificationCodeReturnsBadRequest() {
        string uniqueEmail = $"partial{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithLettersReturnsBadRequest() {
        string uniqueEmail = $"letters{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "abcdef" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationWithNonExistentEmailReturnsBadRequest() {
        string uniqueEmail = $"noexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    // Tests - Verification Replay Attack

    [Fact]
    public void VerificationCodeReusedAfterSuccessReturnsBadRequest() {
        string uniqueEmail = $"replay{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();
        HttpResponseMessage secondVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, secondVerify.StatusCode);
    }

    // Tests - Verification Expiration

    [Fact]
    public void ExpiredVerificationCodeReturnsBadRequest() {
        string uniqueEmail = $"expired{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt9Minutes59SecondsSucceeds() {
        string uniqueEmail = $"justbefore{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9).AddSeconds(-59);
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt10Minutes1SecondReturnsBadRequest() {
        string uniqueEmail = $"justafter{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10).AddSeconds(-1);
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithCorruptedCreatedAtReturnsBadRequest() {
        string uniqueEmail = $"corrupt{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();

        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pendingAccount.CreatedAtUtc = default;
        dbContext.SaveChanges();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }
}
