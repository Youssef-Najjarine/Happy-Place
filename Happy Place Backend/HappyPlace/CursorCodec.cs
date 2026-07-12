using System.Text;

namespace HappyWorld.HappyPlace;

public static class CursorCodec {
    // Methods

    public static string EncodeFeedCursor(byte marker, long primaryKey, long secondaryKey, Guid anchorId) {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"v1|{marker}|{primaryKey}|{secondaryKey}|{anchorId}"));
    }

    public static bool TryDecodeFeedCursor(string cursor, byte expectedMarker, out long primaryKey, out long secondaryKey, out Guid anchorId) {
        primaryKey = 0;
        secondaryKey = 0;
        anchorId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;
        try {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            string[] parts = decoded.Split('|');
            if (parts.Length != 5 || parts[0] != "v1")
                return false;
            if (!byte.TryParse(parts[1], out byte marker) || marker != expectedMarker)
                return false;
            if (!long.TryParse(parts[2], out primaryKey) || primaryKey < 0 || primaryKey > DateTime.MaxValue.Ticks)
                return false;
            if (!long.TryParse(parts[3], out secondaryKey) || secondaryKey < 0 || secondaryKey > DateTime.MaxValue.Ticks)
                return false;
            if (!Guid.TryParse(parts[4], out anchorId))
                return false;
            return true;
        }
        catch (Exception) {
            return false;
        }
    }
}
