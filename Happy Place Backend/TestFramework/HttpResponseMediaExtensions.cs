namespace HappyWorld.HappyPlace;

public static class HttpResponseMediaExtensions {
    // Methods

    public static byte[] ReadContentAsByteArray(this HttpResponseMessage response) {
        return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
    }
}
