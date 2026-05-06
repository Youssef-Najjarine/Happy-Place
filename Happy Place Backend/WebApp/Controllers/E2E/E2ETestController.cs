#if DEBUG
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.WebApp.E2E;

[ApiController]
[Route("api/[controller]/[action]")]
public class E2ETestController : ControllerBase {
    // Fields
    private static bool _initialized;

    // Methods
    private static bool EnsureInitialized() {
        string e2eDatabase = Environment.GetEnvironmentVariable("E2E_TEST_DB");
        if (string.IsNullOrEmpty(e2eDatabase))
            return false;

        if (!_initialized) {
            HappyPlaceDbContext.SetConnectionString(
                $"Server=.;Database={e2eDatabase};Trusted_Connection=True;MultipleActiveResultSets=true;trustservercertificate=yes");
            EmailSender.SetInitializer(() => new E2ENoOperationEmailSender());
            SmsSender.SetInitializer(() => new E2ENoOperationSmsSender());
            _initialized = true;
        }
        return true;
    }

    [HttpGet]
    public IActionResult VerificationCode([FromQuery] string contact) {
        if (!EnsureInitialized())
            return this.NotFound();

        if (string.IsNullOrWhiteSpace(contact))
            return this.BadRequest();

        string trimmedContact = contact.Trim();
        using var dbContext = HappyPlaceDbContext.Create();
        var pendingAccount = dbContext.PendingUserAccounts.SingleOrDefault(
            field => field.EmailAddress == trimmedContact || field.PhoneNumber == trimmedContact);

        if (pendingAccount == null)
            return this.NotFound();

        return this.Ok(new { verificationCode = pendingAccount.VerificationCode });
    }

    [HttpPost]
    public IActionResult ClearDatabase() {
        if (!EnsureInitialized())
            return this.NotFound();

        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.PendingUserAccounts.RemoveRange(dbContext.PendingUserAccounts);
        dbContext.UserAccounts.RemoveRange(dbContext.UserAccounts);
        dbContext.SaveChanges();
        return this.Ok();
    }

    // E2E No-Operation Email Sender
    private class E2ENoOperationEmailSender : EmailSender {
        public override MailMessage NewMailMessage() {
            return new E2ENoOperationMailMessage();
        }

        public override void Send(MailMessage message) {
        }
    }

    private class E2ENoOperationMailMessage : MailMessage {
        public string Subject { get; set; }

        public void AddToAddress(string address) {
        }

        public void AddFromAddress(string address) {
        }

        public void SetHtmlBody(string html) {
        }
    }

    // E2E No-Operation SMS Sender
    private class E2ENoOperationSmsSender : SmsSender {
        public override SmsMessage NewSmsMessage() {
            return new E2ENoOperationSmsMessage();
        }

        public override void Send(SmsMessage message) {
        }
    }

    private class E2ENoOperationSmsMessage : SmsMessage {
        public string ToPhoneNumber { get; set; }
        public string BodyText { get; set; }
    }
}
#endif
