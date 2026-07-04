namespace HappyWorld.HappyPlace.Web.Services;

public static class NotificationSweeper {
    // Fields

    private static readonly int SweepIntervalMs = 500;
    private static readonly Lock StartLock = new();
    private static bool _started;

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
                catch (Exception) {
                }
            }
            Thread.Sleep(SweepIntervalMs);
        }
    }
}
