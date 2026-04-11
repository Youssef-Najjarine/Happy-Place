using System.Text.RegularExpressions;

namespace HappyWorld.HappyPlace.Email;

public class EmailVerificationNotification
{
    private static readonly Regex SixDigits = new(@"\b(\d{6})\b", RegexOptions.Compiled);
    public static string ExtractVerificationCode(MailMessage verificationEmail)
    {
        var bodyProp = verificationEmail.GetType().GetProperty("BodyText");
        var body = bodyProp?.GetValue(verificationEmail) as string ?? string.Empty;

        var m = SixDigits.Match(body);
        Console.WriteLine($"THE 6 DIGITS ARE: {m.Groups[1].Value}");
        if (m.Success) return m.Groups[1].Value;

        throw new InvalidOperationException("Verification code not found in email body.");
    }

    public static void Send(String toAddress, String name, String verificationCode)
    {
        var emailSender = EmailSender.Create();
        MailMessage message = emailSender.NewMailMessage();
        message.AddToAddress(toAddress);
        message.AddFromAddress("youssef@happy.place");
        message.Subject = "Happy Place Verification Code";
        message.SetHtmlBody($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #f4f4f4; padding: 20px; border-radius: 8px;'>
        <h1 style='color: #4a90e2; text-align: center; font-size: 24px; margin-bottom: 10px;'>Welcome to Happy Place!</h1>
        <p style='font-size: 16px; margin-bottom: 20px;'>Hi <strong>{name}</strong>,</p>
        <p style='font-size: 16px; margin-bottom: 30px;'>To get started, please use the 6-digit verification code below:</p>
        
        <div style='text-align: center; background-color: #ffffff; padding: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
            <span style='font-size: 32px; font-weight: bold; color: #4a90e2; letter-spacing: 5px;'>{verificationCode}</span>
        </div>
        
        <p style='font-size: 14px; color: #666; margin-top: 20px;'>This code will expire in 10 minutes. If you didn't request this, please ignore this email.</p>
        
        <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
        <p style='font-size: 12px; color: #888; text-align: center;'>Best regards,<br><strong>Happy Place Team</strong></p>
        <p style='font-size: 12px; color: #888; text-align: center;'><a href='https://happy.place' style='color: #4a90e2;'>happy.place</a></p>
    </div>
</body>
</html>
            ");
        emailSender.Send(message);
    }
}
