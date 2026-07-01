namespace HappyWorld.HappyPlace.PushNotifications;

public class PushMessage {
    // Properties
    public string Token { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];
    public string CollapseId { get; set; }
    public Boolean Alerting { get; set; } = true;
    public Boolean IsDismiss { get; set; }
}
