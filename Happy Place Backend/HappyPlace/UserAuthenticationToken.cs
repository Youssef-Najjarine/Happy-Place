using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;

namespace HappyWorld.HappyPlace;

public class UserAuthenticationToken {
    // Fields
    private static readonly int TokenExpirationDays = 7;
    private static readonly int IvSizeBytes = 16;

    // Constructors
    static UserAuthenticationToken() {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
            directory = directory.Parent;
        if (directory != null) Env.Load(Path.Combine(directory.FullName, ".env"));
    }

    private UserAuthenticationToken(string identifier) {
        this.Identifier = identifier;
        this.ExpirationDateUtc = DateTimeOffset.UtcNow.AddDays(TokenExpirationDays);
    }

    [JsonConstructor]
    private UserAuthenticationToken() { }

    // Properties
    public string Identifier { get; set; }
    public DateTimeOffset ExpirationDateUtc { get; set; }

    // Methods
    public static UserAuthenticationToken GenerateForUser(string identifier) {
        return new(identifier);
    }

    public static UserAuthenticationToken ValidateToken(string authTokenString) {
        byte[] key = GetEncryptionKey();
        byte[] encryptedWithIv = Convert.FromBase64String(authTokenString);

        if (encryptedWithIv.Length <= IvSizeBytes)
            return null;

        byte[] iv = new byte[IvSizeBytes];
        byte[] encryptedPayload = new byte[encryptedWithIv.Length - IvSizeBytes];
        Buffer.BlockCopy(encryptedWithIv, 0, iv, 0, IvSizeBytes);
        Buffer.BlockCopy(encryptedWithIv, IvSizeBytes, encryptedPayload, 0, encryptedPayload.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var stream = new MemoryStream(encryptedPayload);
        using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
        string decryptedText = reader.ReadToEnd();

        UserAuthenticationToken token = JsonSerializer.Deserialize<UserAuthenticationToken>(decryptedText);
        if (token == null)
            return null;

        if (token.ExpirationDateUtc < DateTimeOffset.UtcNow)
            return null;

        return token;
    }

    public string ToAuthTokenString() {
        byte[] key = GetEncryptionKey();
        string decryptedText = JsonSerializer.Serialize(this);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var stream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write)) {
            using var writer = new StreamWriter(cryptoStream, Encoding.UTF8);
            writer.Write(decryptedText);
        }

        byte[] encryptedPayload = stream.ToArray();
        byte[] encryptedWithIv = new byte[aes.IV.Length + encryptedPayload.Length];
        Buffer.BlockCopy(aes.IV, 0, encryptedWithIv, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedPayload, 0, encryptedWithIv, aes.IV.Length, encryptedPayload.Length);

        return Convert.ToBase64String(encryptedWithIv);
    }

    private static byte[] GetEncryptionKey() {
        string keyBase64 = Environment.GetEnvironmentVariable("AUTH_TOKEN_KEY");
        if (string.IsNullOrEmpty(keyBase64))
            throw new InvalidOperationException("AUTH_TOKEN_KEY environment variable is not set.");
        return Convert.FromBase64String(keyBase64);
    }
}
