namespace HappyWorld.HappyPlace.Email;

public class EmailEmailChangedNotification {
    // Methods
    public static void Send(String toAddress, String name) {
        var emailSender = EmailSender.Create();
        MailMessage message = emailSender.NewMailMessage();
        message.AddToAddress(toAddress);
        message.AddFromAddress("noreply@happy.place");
        message.Subject = "Security Alert: Your Happy Place Email Was Changed";
        message.SetHtmlBody($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: rgba(35, 35, 35, 1.0); max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #F9F9F9; padding: 20px; border-radius: 8px;'>
        <h1 style='color: #ED5370; text-align: center; font-size: 24px; margin-bottom: 10px;'>Email Address Changed</h1>
        <p style='font-size: 16px; margin-bottom: 20px;'>Hi <strong>{name}</strong>,</p>
        <p style='font-size: 16px; margin-bottom: 20px;'>The email address on your Happy Place account was just changed.</p>
        
        <div style='background-color: #FFFFFF; padding: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
            <p style='font-size: 16px; margin: 0;'><strong>If you made this change</strong>, no further action is needed.</p>
            <p style='font-size: 16px; margin-top: 16px; margin-bottom: 0;'><strong>If you did not make this change</strong>, your account may be compromised. Please contact support immediately.</p>
        </div>
        
        <p style='font-size: 14px; color: rgba(35, 35, 35, 0.50); margin-top: 20px;'>This is an automated security alert sent to the previous email address on your account.</p>
        
        <hr style='border: none; border-top: 1px solid #F9F9F9; margin: 30px 0;'>
        <p style='font-size: 12px; color: rgba(35, 35, 35, 0.50); text-align: center;'>Best regards,<br><strong>Happy Place Team</strong></p>
        <p style='font-size: 12px; color: rgba(35, 35, 35, 0.50); text-align: center;'><a href='https://happy.place/support' target='_blank' style='color: #ED5370;'>happy.place/support</a></p>
    </div>
</body>
</html>
            ");
        emailSender.Send(message);
    }
}
