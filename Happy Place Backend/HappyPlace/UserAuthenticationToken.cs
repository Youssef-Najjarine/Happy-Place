using System.Security.Cryptography;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

public class UserAuthenticationToken
{
    // Constructors
    private UserAuthenticationToken(string email)
    {
        this.Username = email;
        this.ExpirationDate = DateTimeOffset.Now.AddMinutes(20);
    }

    // Properties
    public string Username { get; }
    public DateTimeOffset ExpirationDate { get; }

    // Methods
    public static UserAuthenticationToken GenerateForUser(string email)
    {
        // To do retrieve from the users table when I create it.
        // should return new(userRecord.Username, userRecord.Id, userRecord.....)
        return new(email);
    }
    public string ToAuthTokenString()
    {
       var decryptedText = JsonSerializer.Serialize(this);
        // Encrypt using aes
        var aes = Aes.Create();
        // TODO: Store key and IV securely
        aes.Key = Convert.FromBase64String("bWluZHN0b25lX2lzX3RoZV9iZXN0X3NlY3JldF9rZXk=");
        aes.IV = new byte[16]; // Zero IV for simplicity, use a random IV in production
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var stream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
        {
          using var writer = new StreamWriter(cryptoStream);
          writer.Write(decryptedText);
          cryptoStream.FlushFinalBlock();
        }
        return Convert.ToBase64String(stream.ToArray());
    }
}
