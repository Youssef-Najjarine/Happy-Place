namespace HappyWorld.HappyPlace.Sms;

public interface SmsMessage {
    string ToPhoneNumber { get; set; }
    string BodyText { get; set; }
}