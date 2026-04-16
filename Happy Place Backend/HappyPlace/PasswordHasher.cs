using System.Security.Cryptography;

public static class PasswordHasher {
    private const int SaltSize = 16; // 128 bits (base64: 24 chars)
    private const int HashSize = 32; // 256 bits (base64: 44 chars)
    private const int DefaultIterations = 600000; // ~6 chars; tune for ~250ms
    private const int MaxStoredLength = 100; // Your DB limit

    public static string HashPassword(string password, int? customIterations = null) {
        int iterations = customIterations ?? DefaultIterations;
        if (iterations <= 0) throw new ArgumentException("Iterations must be positive.");

        // Generate unique salt
        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        // Derive hash using string overload (UTF-8 encoded internally)
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,  // Direct string—no manual encoding needed
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,  // Matches HMAC-SHA512 PRF
            outputLength: HashSize  // Fixed: was 'numBytesRequested'
        );

        // Build formatted string
        string iterStr = iterations.ToString();
        string saltB64 = Convert.ToBase64String(salt);
        string hashB64 = Convert.ToBase64String(hash);
        string fullHash = $"{iterStr}.{saltB64}.{hashB64}";

        // Enforce length (rarely needed, but safe)
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
            return false; // Invalid base64
        }

        // Re-derive and compare (constant-time)
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,  // Direct string
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: storedHashBytes.Length  // Fixed: was 'numBytesRequested'
        );

        return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
    }
}