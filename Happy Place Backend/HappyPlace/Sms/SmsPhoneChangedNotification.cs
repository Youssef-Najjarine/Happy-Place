namespace HappyWorld.HappyPlace.Sms;

public class SmsPhoneChangedNotification {
    // Methods
    public static void Send(string toPhoneNumber, string name) {
        var smsSender = SmsSender.Create();
        SmsMessage message = smsSender.NewSmsMessage();
        message.ToPhoneNumber = toPhoneNumber;
        message.BodyText = $"Hi {name}, your Happy Place phone number was just changed. If you didn't make this change, please contact support immediately at happy.place/support.\n\n@happy.place";
        smsSender.Send(message);
    }
}
