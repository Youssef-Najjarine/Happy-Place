namespace HappyWorld.HappyPlace.Realtime;

public abstract class RealtimeSender {
    // Fields
    private static Func<RealtimeSender> _initializer;

    // Methods
    public static void ResetInitializer() => SetInitializer(null);

    public static void SetInitializer(Func<RealtimeSender> initializer) {
        _initializer = initializer;
    }

    internal static RealtimeSender Create() {
        if (_initializer != null) return _initializer();
        return new NoOpRealtimeSender();
    }

    public abstract void SendToGroup(string groupName, string eventName, Dictionary<string, string> payload);

    public abstract void SendToGroups(List<string> groupNames, string eventName, Dictionary<string, string> payload);
}
