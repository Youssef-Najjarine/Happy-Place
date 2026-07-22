namespace HappyWorld.HappyPlace.Web.Services;

public static class NotificationSweeper {
    // Fields

    private static readonly int SweepIntervalMs = 500;
    private static readonly int RetentionIntervalMs = 3600000;
    private static readonly Lock StartLock = new();
    private static bool _started;
    private static DateTime _lastRetentionSweepAtUtc = DateTime.MinValue;

    // Methods

    public static void Start() {
        lock (StartLock) {
            if (_started)
                return;
            _started = true;
            Thread sweepThread = new(RunSweepLoop) {
                IsBackground = true,
                Name = "NotificationSweeper"
            };
            sweepThread.Start();
        }
    }

    private static void RunSweepLoop() {
        while (true) {
            if (NotificationDispatchManager.BackgroundSweepEnabled) {
                try {
                    NotificationDispatchManager.Sweep();
                }
                catch (Exception exception) {
                    Console.Error.WriteLine($"Notification sweep failed: {exception.Message}");
                }
                if ((DateTime.UtcNow - _lastRetentionSweepAtUtc).TotalMilliseconds >= RetentionIntervalMs) {
                    _lastRetentionSweepAtUtc = DateTime.UtcNow;
                    try {
                        NotificationDispatchManager.RunRetentionSweep();
                    }
                    catch (Exception exception) {
                        Console.Error.WriteLine($"Notification retention sweep failed: {exception.Message}");
                    }
                }
            }
            Thread.Sleep(SweepIntervalMs);
        }
    }
}
