using HappyWorld.HappyPlace.Sms;

namespace HappyWorld.HappyPlace;

public class InMemorySmsSender : SmsSender
{
    // Properties
    public IList<SmsMessage> SentMessages { get; } = [];

    // Methods
    public override SmsMessage NewSmsMessage()
    {
        return new InMemorySmsMessage();
    }

    public override void Send(SmsMessage message)
    {
        this.SentMessages.Add(message);
    }
}