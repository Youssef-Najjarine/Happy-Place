using System.Text.Json;

namespace HappyWorld.HappyPlace {
    public static class HttpResponseMessageExtensions {
        public static JsonDocument ReadContentAsJsonDocument(this HttpResponseMessage responseMessage) {
            Console.WriteLine("HttpResponseMessageExtensions RESPONSEmESSAGE: " + responseMessage);
            string contentString = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.WriteLine("HTTPResponse Content: " + contentString);
            return JsonDocument.Parse(contentString);
        }
    }
}
