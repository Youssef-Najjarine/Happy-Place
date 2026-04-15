using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using WebApp;

namespace HappyWorld.HappyPlace
{
    public class WebClient : IDisposable
    {
        // Fields
        private HttpClient _httpClient;
        private WebApplicationFactory<Program> _webApplicationFactory;

        // Constructors
        public WebClient()
        {
            this._webApplicationFactory = new WebApplicationFactory<Program>();
            this._httpClient = this._webApplicationFactory.CreateClient();
        }

        // Methods

        public void Dispose()
        {
            this._httpClient?.Dispose();
            this._httpClient = null;
            this._webApplicationFactory?.Dispose();
            this._webApplicationFactory = null;
        }

        public HttpResponseMessage PostJson(string url, object jsonData)
        {
            return this._httpClient.PostAsync(url, JsonContent.Create(jsonData)).GetAwaiter().GetResult();
        }
    }
}
