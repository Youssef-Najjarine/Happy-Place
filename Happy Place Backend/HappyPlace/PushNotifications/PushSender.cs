namespace HappyWorld.HappyPlace.PushNotifications;

public abstract class PushSender {
    // Fields
    private static Func<PushSender> _initializer;

    // Methods
    public static void ResetInitializer() => SetInitializer(null);

    public static void SetInitializer(Func<PushSender> initializer) {
        _initializer = initializer;
    }

    internal static PushSender Create() {
        if (_initializer != null) return _initializer();
        return new FirebasePushSender();
    }

    public abstract void Send(PushMessage message);
}
