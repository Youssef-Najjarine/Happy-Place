using System.Text.Json;

namespace HappyWorld.HappyPlace {
    public static class HttpResponseMessageExtensions {
        public static JsonDocument ReadContentAsJsonDocument(this HttpResponseMessage responseMessage) {
            string contentString = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            try {
                return JsonDocument.Parse(contentString);
            }
            catch (JsonException jsonException) {
                throw new InvalidOperationException($"Response was not JSON. Status={(int)responseMessage.StatusCode} {responseMessage.StatusCode}. Body={contentString}", jsonException);
            }
        }
    }
}
