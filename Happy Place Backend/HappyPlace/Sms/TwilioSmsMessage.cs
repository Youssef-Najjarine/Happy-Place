namespace HappyWorld.HappyPlace.Sms;

internal class TwilioSmsMessage : SmsMessage {
    // Properties
    public string ToPhoneNumber { get; set; }
    public string BodyText { get; set; }
}
