namespace HappyWorld.HappyPlace;

public record RealtimeSentEvent(string GroupName, string EventName, Dictionary<string, string> Payload);
