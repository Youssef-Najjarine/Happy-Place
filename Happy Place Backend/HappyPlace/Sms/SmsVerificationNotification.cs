using System.Text.RegularExpressions;

namespace HappyWorld.HappyPlace.Sms;

public class SmsVerificationNotification {
    // Fields
    private static readonly Regex SixDigits = new(@"\b(\d{6})\b", RegexOptions.Compiled);

    // Methods
    public static string ExtractVerificationCode(SmsMessage verificationSms) {
        string body = verificationSms.BodyText ?? string.Empty;
        var match = SixDigits.Match(body);
        if (match.Success) return match.Groups[1].Value;
        throw new InvalidOperationException("Verification code not found in SMS body.");
    }

    public static void Send(string toPhoneNumber, string name, string verificationCode) {
        var smsSender = SmsSender.Create();
        SmsMessage message = smsSender.NewSmsMessage();
        message.ToPhoneNumber = toPhoneNumber;
        message.BodyText = $"Your Happy Place verification code is: {verificationCode}. This code expires in 10 minutes.\n\n@happy.place #{verificationCode}";
        smsSender.Send(message);
    }
}
