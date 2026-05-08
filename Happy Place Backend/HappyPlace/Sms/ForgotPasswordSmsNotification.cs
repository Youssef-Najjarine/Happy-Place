namespace HappyWorld.HappyPlace.Sms;

public class ForgotPasswordSmsNotification {
    // Methods
    public static void Send(string toPhoneNumber, string name, string verificationCode) {
        var smsSender = SmsSender.Create();
        SmsMessage message = smsSender.NewSmsMessage();
        message.ToPhoneNumber = toPhoneNumber;
        message.BodyText = $"Hi {name}, your Happy Place password reset code is: {verificationCode}. This code expires in 10 minutes.\n\n@happy.place #{verificationCode}";
        smsSender.Send(message);
    }
}
