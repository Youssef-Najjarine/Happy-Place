namespace HappyWorld.HappyPlace.Email;

public class EmailAccountVerifiedNotification {
    // Methods
    public static void Send(string toAddress, string name) {
        var emailSender = EmailSender.Create();
        MailMessage message = emailSender.NewMailMessage();
        message.AddToAddress(toAddress);
        message.AddFromAddress("noreply@happy.place");
        message.Subject = "Welcome to Happy Place!";
        message.SetHtmlBody($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: rgba(35, 35, 35, 1.0); max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background-color: #F9F9F9; padding: 20px; border-radius: 8px;'>
        <h1 style='color: #ED5370; text-align: center; font-size: 24px; margin-bottom: 10px;'>Account Verified!</h1>
        <p style='font-size: 16px; margin-bottom: 20px;'>Hi <strong>{name}</strong>,</p>
        <p style='font-size: 16px; margin-bottom: 20px;'>Your Happy Place account has been verified successfully. You're all set to start using the app!</p>
        <p style='font-size: 16px; margin-bottom: 30px;'>If you did not create this account, please contact our support team immediately.</p>
        
        <hr style='border: none; border-top: 1px solid #F9F9F9; margin: 30px 0;'>
        <p style='font-size: 12px; color: rgba(35, 35, 35, 0.50); text-align: center;'>Best regards,<br><strong>Happy Place Team</strong></p>
        <p style='font-size: 12px; color: rgba(35, 35, 35, 0.50); text-align: center;'><a href='https://happy.place' target='_blank' style='color: #ED5370;'>happy.place</a></p>
    </div>
</body>
</html>
            ");
        emailSender.Send(message);
    }
}
