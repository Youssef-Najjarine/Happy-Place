using System.Text.Json;
using System.Text.Json.Serialization;

namespace HappyWorld.HappyPlace.Web.Json;

public class UtcDateTimeJsonConverter : JsonConverter<DateTime> {
    // Methods

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        DateTime parsedValue = reader.GetDateTime();
        if (parsedValue.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(parsedValue, DateTimeKind.Utc);
        return parsedValue.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        DateTime utcValue = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        writer.WriteStringValue(utcValue);
    }
}
