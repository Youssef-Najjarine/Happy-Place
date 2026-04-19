namespace HappyWorld.HappyPlace.Sms;

public class SmsAccountVerifiedNotification {
    // Methods
    public static void Send(string toPhoneNumber, string name) {
        var smsSender = SmsSender.Create();
        SmsMessage message = smsSender.NewSmsMessage();
        message.ToPhoneNumber = toPhoneNumber;
        message.BodyText = $"Hi {name}, your Happy Place account has been verified successfully. Welcome to Happy Place!";
        smsSender.Send(message);
    }
}
