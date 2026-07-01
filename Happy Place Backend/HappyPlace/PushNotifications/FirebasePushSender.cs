using DotNetEnv;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HappyWorld.HappyPlace.PushNotifications;

internal class FirebasePushSender : PushSender {
    // Fields

    private static readonly HttpClient SharedHttpClient = new();
    private static readonly Lock CredentialLock = new();
    private static readonly string MessagingScope = "https://www.googleapis.com/auth/firebase.messaging";
    private static readonly string DefaultTokenUri = "https://oauth2.googleapis.com/token";
    private static readonly int TokenExpiryBufferSeconds = 60;
    private static readonly string _envFileDirectory;
    private static ServiceAccount _serviceAccount;
    private static string _cachedAccessToken;
    private static DateTime _cachedAccessTokenExpiryUtc;

    // Constructors

    static FirebasePushSender() {
        DirectoryInfo directory = new(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
            directory = directory.Parent;
        if (directory != null) {
            _envFileDirectory = directory.FullName;
            Env.Load(Path.Combine(directory.FullName, ".env"));
        }
    }

    // Methods

    public override void Send(PushMessage message) {
        ServiceAccount serviceAccount = GetServiceAccount();
        string accessToken = GetAccessToken(serviceAccount);
        string requestUri = $"https://fcm.googleapis.com/v1/projects/{serviceAccount.ProjectId}/messages:send";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(BuildMessageBody(message), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = SharedHttpClient.Send(request);
        if (response.IsSuccessStatusCode)
            return;

        string responseBody = ReadBody(response);
        if (IsInvalidTokenResponse(response.StatusCode, responseBody))
            throw new PushTokenInvalidException(message.Token);
        throw new HttpRequestException($"FCM v1 send failed ({(int)response.StatusCode}): {responseBody}");
    }

    private static string BuildMessageBody(PushMessage message) {
        Dictionary<string, object> fcmMessage = new() {
            ["token"] = message.Token,
            ["data"] = message.Data
        };

        Dictionary<string, object> android = [];
        Dictionary<string, object> apnsHeaders = [];
        Dictionary<string, object> aps = [];

        if (!string.IsNullOrEmpty(message.CollapseId)) {
            android["collapse_key"] = message.CollapseId;
            apnsHeaders["apns-collapse-id"] = message.CollapseId;
        }

        if (message.IsDismiss) {
            android["priority"] = "high";
            aps["content-available"] = 1;
            apnsHeaders["apns-push-type"] = "background";
            apnsHeaders["apns-priority"] = "5";
        }
        else {
            fcmMessage["notification"] = new Dictionary<string, object> {
                ["title"] = message.Title,
                ["body"] = message.Body
            };
            Dictionary<string, object> androidNotification = [];
            if (!string.IsNullOrEmpty(message.CollapseId))
                androidNotification["tag"] = message.CollapseId;
            android["priority"] = "high";
            apnsHeaders["apns-push-type"] = "alert";
            if (message.Alerting) {
                androidNotification["sound"] = "default";
                androidNotification["notification_priority"] = "PRIORITY_HIGH";
                aps["sound"] = "default";
                aps["interruption-level"] = "active";
                apnsHeaders["apns-priority"] = "10";
            }
            else {
                androidNotification["notification_priority"] = "PRIORITY_LOW";
                aps["interruption-level"] = "passive";
                apnsHeaders["apns-priority"] = "5";
            }
            if (androidNotification.Count > 0)
                android["notification"] = androidNotification;
        }

        if (aps.Count > 0 || apnsHeaders.Count > 0) {
            Dictionary<string, object> apns = [];
            if (apnsHeaders.Count > 0)
                apns["headers"] = apnsHeaders;
            if (aps.Count > 0)
                apns["payload"] = new Dictionary<string, object> { ["aps"] = aps };
            fcmMessage["apns"] = apns;
        }
        if (android.Count > 0)
            fcmMessage["android"] = android;

        Dictionary<string, object> payload = new() {
            ["message"] = fcmMessage
        };
        return JsonSerializer.Serialize(payload);
    }

    private static ServiceAccount GetServiceAccount() {
        lock (CredentialLock) {
            _serviceAccount ??= LoadServiceAccount();
            return _serviceAccount;
        }
    }

    private static string GetAccessToken(ServiceAccount serviceAccount) {
        lock (CredentialLock) {
            if (_cachedAccessToken != null && DateTime.UtcNow < _cachedAccessTokenExpiryUtc)
                return _cachedAccessToken;
            string assertion = BuildSignedJwt(serviceAccount);
            string tokenResponse = ExchangeJwtForAccessToken(serviceAccount.TokenUri, assertion);
            using JsonDocument document = JsonDocument.Parse(tokenResponse);
            string accessToken = document.RootElement.GetProperty("access_token").GetString();
            int expiresInSeconds = document.RootElement.GetProperty("expires_in").GetInt32();
            _cachedAccessToken = accessToken;
            _cachedAccessTokenExpiryUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds - TokenExpiryBufferSeconds);
            return accessToken;
        }
    }

    private static string ExchangeJwtForAccessToken(string tokenUri, string assertion) {
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUri);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
            ["assertion"] = assertion
        });
        using HttpResponseMessage response = SharedHttpClient.Send(request);
        string responseBody = ReadBody(response);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"FCM OAuth token request failed ({(int)response.StatusCode}): {responseBody}");
        return responseBody;
    }

    private static string BuildSignedJwt(ServiceAccount serviceAccount) {
        long issuedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiresAtUnixSeconds = issuedAtUnixSeconds + 3600;
        string encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        string encodedClaims = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new {
            iss = serviceAccount.ClientEmail,
            scope = MessagingScope,
            aud = serviceAccount.TokenUri,
            iat = issuedAtUnixSeconds,
            exp = expiresAtUnixSeconds
        }));
        string signingInput = $"{encodedHeader}.{encodedClaims}";
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(serviceAccount.PrivateKey);
        byte[] signature = rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static ServiceAccount LoadServiceAccount() {
        string inlineJson = Environment.GetEnvironmentVariable("FCM_SERVICE_ACCOUNT_JSON");
        string json = string.IsNullOrWhiteSpace(inlineJson) ? ReadServiceAccountFile() : inlineJson;
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        return new(
            root.GetProperty("project_id").GetString(),
            root.GetProperty("client_email").GetString(),
            root.GetProperty("private_key").GetString(),
            root.TryGetProperty("token_uri", out JsonElement tokenUriElement) ? tokenUriElement.GetString() : DefaultTokenUri);
    }

    private static string ReadServiceAccountFile() {
        string path = Environment.GetEnvironmentVariable("FCM_SERVICE_ACCOUNT_PATH");
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Firebase credentials are not configured. Set FCM_SERVICE_ACCOUNT_JSON (inline JSON) or FCM_SERVICE_ACCOUNT_PATH (path to the service-account JSON file, relative to the .env file or absolute).");
        return File.ReadAllText(ResolveServiceAccountPath(path));
    }

    private static string ResolveServiceAccountPath(string path) {
        if (Path.IsPathRooted(path))
            return path;
        if (_envFileDirectory != null)
            return Path.GetFullPath(Path.Combine(_envFileDirectory, path));
        return path;
    }

    private static bool IsInvalidTokenResponse(HttpStatusCode statusCode, string responseBody) {
        string errorCode = ExtractFcmErrorCode(responseBody);
        if (errorCode == "UNREGISTERED" || errorCode == "INVALID_ARGUMENT" || errorCode == "SENDER_ID_MISMATCH")
            return true;
        if (errorCode != null)
            return false;
        return statusCode == HttpStatusCode.NotFound;
    }

    private static string ExtractFcmErrorCode(string responseBody) {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;
        try {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out JsonElement error))
                return null;
            if (!error.TryGetProperty("details", out JsonElement details) || details.ValueKind != JsonValueKind.Array)
                return null;
            foreach (JsonElement detail in details.EnumerateArray()) {
                if (detail.TryGetProperty("errorCode", out JsonElement errorCode))
                    return errorCode.GetString();
            }
            return null;
        }
        catch (JsonException) {
            return null;
        }
    }

    private static string ReadBody(HttpResponseMessage response) {
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }

    private static string Base64UrlEncode(byte[] bytes) {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed record ServiceAccount(string ProjectId, string ClientEmail, string PrivateKey, string TokenUri);
}
