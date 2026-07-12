using System.Security.Cryptography;
using System.Text;

namespace HappyWorld.HappyPlace;

public static class MessageCipher {
    // Fields

    private static readonly string DevelopmentKeyBase64 = "nZd+qdh3a5w59jWs9EMhXaIHnB2RTMWT3XfMmynp49U=";
    private static readonly int NonceSizeBytes = 12;
    private static readonly int TagSizeBytes = 16;
    private static string _keyOverrideBase64;

    public static readonly byte CurrentVersion = 1;

    // Methods

    public static void SetKey(string keyBase64) => _keyOverrideBase64 = keyBase64;

    public static byte[] Encrypt(string plaintext) {
        return EncryptBytes(Encoding.UTF8.GetBytes(plaintext));
    }

    public static string Decrypt(byte[] envelope) {
        byte[] plainBytes = DecryptBytes(envelope);
        if (plainBytes == null)
            return null;
        return Encoding.UTF8.GetString(plainBytes);
    }

    public static byte[] EncryptBytes(byte[] plainBytes) {
        byte[] key = Convert.FromBase64String(_keyOverrideBase64 ?? DevelopmentKeyBase64);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] tag = new byte[TagSizeBytes];
        byte[] cipherBytes = new byte[plainBytes.Length];
        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        byte[] envelope = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
        nonce.CopyTo(envelope, 0);
        tag.CopyTo(envelope, NonceSizeBytes);
        cipherBytes.CopyTo(envelope, NonceSizeBytes + TagSizeBytes);
        return envelope;
    }

    public static byte[] DecryptBytes(byte[] envelope) {
        if (envelope == null)
            return null;
        byte[] key = Convert.FromBase64String(_keyOverrideBase64 ?? DevelopmentKeyBase64);
        byte[] nonce = envelope[..NonceSizeBytes];
        byte[] tag = envelope[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        byte[] cipherBytes = envelope[(NonceSizeBytes + TagSizeBytes)..];
        byte[] plainBytes = new byte[cipherBytes.Length];
        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        return plainBytes;
    }
}
