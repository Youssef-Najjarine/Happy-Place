using DotNetEnv;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace HappyWorld.HappyPlace.Sms;

internal class TwilioSmsSender : SmsSender {
    // Constructors
    static TwilioSmsSender() {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
            directory = directory.Parent;
        if (directory != null) Env.Load(Path.Combine(directory.FullName, ".env"));

        string accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
        string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
        TwilioClient.Init(accountSid, authToken);
    }

    // Methods
    public override SmsMessage NewSmsMessage() {
        return new TwilioSmsMessage();
    }

    public override void Send(SmsMessage message) {
        string fromPhone = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE");
        string toPhone = FormatToE164(message.ToPhoneNumber);

        MessageResource.Create(
            to: new PhoneNumber(toPhone),
            from: new PhoneNumber(fromPhone),
            body: message.BodyText
        );
    }

    private static string FormatToE164(string phoneNumber) {
        if (phoneNumber.StartsWith("+"))
            return phoneNumber;
        return $"+1{phoneNumber}";
    }
}
