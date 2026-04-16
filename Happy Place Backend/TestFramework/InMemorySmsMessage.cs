using HappyWorld.HappyPlace.Sms;

namespace HappyWorld.HappyPlace;

public class InMemorySmsMessage : SmsMessage {
    // Properties
    public string ToPhoneNumber { get; set; }
    public string BodyText { get; set; }
}