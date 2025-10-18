using System.Text.Json;

namespace HappyWorld.HappyPlace
{
    public static class HttpResponseMessageExtensions
    {
        public static JsonDocument ReadContentAsJsonDocument(this HttpResponseMessage responseMessage)
        {
            string contentString = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonDocument.Parse(contentString);
        }
    }
}
