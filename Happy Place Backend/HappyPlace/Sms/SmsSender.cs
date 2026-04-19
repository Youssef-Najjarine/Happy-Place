namespace HappyWorld.HappyPlace.Sms;

public abstract class SmsSender {
    // Fields
    private static Func<SmsSender> _initializer;

    // Methods
    public static void ResetInitializer() => SetInitializer(null);

    public static void SetInitializer(Func<SmsSender> initializer) {
        _initializer = initializer;
    }

    internal static SmsSender Create() {
        if (_initializer != null) return _initializer();
        return new TwilioSmsSender();
    }

    public abstract SmsMessage NewSmsMessage();
    public abstract void Send(SmsMessage message);
}
