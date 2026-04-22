using System.Text.RegularExpressions;

namespace HappyWorld.HappyPlace.Sms;

public class SmsVerificationNotification {
    // Fields
    private const string SixDigitPattern = @"\b(\d{6})\b";

    // Methods
    public static string ExtractVerificationCode(SmsMessage verificationSms) {
        string body = verificationSms.BodyText ?? string.Empty;
        var verificationCodeMatch = Regex.Match(body, SixDigitPattern);
        if (verificationCodeMatch.Success) return verificationCodeMatch.Groups[1].Value;
        throw new InvalidOperationException("Verification code not found in SMS body.");
    }

    public static void Send(string toPhoneNumber, string name, string verificationCode) {
        var smsSender = SmsSender.Create();
        SmsMessage message = smsSender.NewSmsMessage();
        message.ToPhoneNumber = toPhoneNumber;
        message.BodyText = $"Hi {name}, your Happy Place verification code is: {verificationCode}. This code expires in 10 minutes.\n\n@happy.place #{verificationCode}";
        smsSender.Send(message);
    }
}
