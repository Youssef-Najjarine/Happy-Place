using System.Security.Cryptography;

namespace HappyWorld.HappyPlace;

public static class PasswordHasher {
    // Fields
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 600000;
    private const int MaxStoredLength = 100;

    // Methods
    public static string HashPassword(string password, int? customIterations = null) {
        int iterations = customIterations ?? DefaultIterations;
        if (iterations <= 0) throw new ArgumentException("Iterations must be positive.");

        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: HashSize
        );

        string iterStr = iterations.ToString();
        string saltB64 = Convert.ToBase64String(salt);
        string hashB64 = Convert.ToBase64String(hash);
        string fullHash = $"{iterStr}.{saltB64}.{hashB64}";

        if (fullHash.Length > MaxStoredLength)
            throw new InvalidOperationException($"Hash too long ({fullHash.Length} chars). Reduce iterations or salt size.");

        return fullHash;
    }

    public static bool VerifyPassword(string password, string storedHash) {
        if (string.IsNullOrEmpty(storedHash) || storedHash.Length > MaxStoredLength) return false;

        var parts = storedHash.Split('.');
        if (parts.Length != 3) return false;
        if (!int.TryParse(parts[0], out int iterations)) return false;

        byte[] salt;
        byte[] storedHashBytes;
        try {
            salt = Convert.FromBase64String(parts[1]);
            storedHashBytes = Convert.FromBase64String(parts[2]);
        }
        catch {
            return false;
        }

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: storedHashBytes.Length
        );

        return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
    }
}
