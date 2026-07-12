using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using HappyWorld.HappyPlace.WebApp;
namespace HappyWorld.HappyPlace {
    public class WebClient : IDisposable {
        // Fields
        private HttpClient _httpClient;
        private WebApplicationFactory<Program> _webApplicationFactory;
        // Constructors
        public WebClient() {
            this._webApplicationFactory = new WebApplicationFactory<Program>();
            this._httpClient = this._webApplicationFactory.CreateClient();
        }
        // Methods
        public void Dispose() {
            GC.SuppressFinalize(this);
            this._httpClient?.Dispose();
            this._httpClient = null;
            this._webApplicationFactory?.Dispose();
            this._webApplicationFactory = null;
        }
        public HttpResponseMessage PostJson(string url, object jsonData) {
            return this._httpClient.PostAsync(url, JsonContent.Create(jsonData)).GetAwaiter().GetResult();
        }
        public HttpResponseMessage Get(string url) {
            return this._httpClient.GetAsync(url).GetAwaiter().GetResult();
        }
        public HttpResponseMessage GetWithRange(string url, long fromByte, long toByte) {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(fromByte, toByte);
            return this._httpClient.SendAsync(request).GetAwaiter().GetResult();
        }
        public HttpResponseMessage UploadMultipart(string url, Dictionary<string, string> formFields, params (string FieldName, byte[] Content, string FileName, string ContentType)[] files) {
            using var multipartContent = new MultipartFormDataContent();
            foreach (var (fieldKey, fieldValue) in formFields) {
                multipartContent.Add(new StringContent(fieldValue), fieldKey);
            }
            foreach (var (FieldName, Content, FileName, ContentType) in files) {
                var fileContent = new ByteArrayContent(Content);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
                multipartContent.Add(fileContent, FieldName, FileName);
            }
            return this._httpClient.PostAsync(url, multipartContent).GetAwaiter().GetResult();
        }
    }
}
