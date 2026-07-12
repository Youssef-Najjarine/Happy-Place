using System.Security.Cryptography;
using System.Text;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class MessageCipherTest {
    // Tests

    [Fact]
    public void EncryptDecryptRoundTripsUtf8() {
        string plaintext = "Hello from HappyPlace \u00e9\u00e8\u00fc \u4f60\u597d \ud83d\ude0a\ud83c\udf89";

        byte[] envelope = MessageCipher.Encrypt(plaintext);
        string decrypted = MessageCipher.Decrypt(envelope);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void NoncesAreUniqueAcrossEncryptions() {
        string plaintext = "same text " + Guid.NewGuid();

        byte[] firstEnvelope = MessageCipher.Encrypt(plaintext);
        byte[] secondEnvelope = MessageCipher.Encrypt(plaintext);

        Assert.False(firstEnvelope.SequenceEqual(secondEnvelope));
        Assert.False(firstEnvelope.AsSpan(0, 12).SequenceEqual(secondEnvelope.AsSpan(0, 12)));
    }

    [Fact]
    public void TamperedCiphertextThrows() {
        byte[] envelope = MessageCipher.Encrypt("do not tamper " + Guid.NewGuid());
        envelope[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => MessageCipher.Decrypt(envelope));
    }

    [Fact]
    public void CiphertextDiffersFromPlaintextBytes() {
        string plaintext = "sensitive support message " + Guid.NewGuid();

        byte[] envelope = MessageCipher.Encrypt(plaintext);

        Assert.False(envelope.SequenceEqual(Encoding.UTF8.GetBytes(plaintext)));
    }

    [Fact]
    public void DecryptNullReturnsNull() {
        Assert.Null(MessageCipher.Decrypt(null));
    }

    [Fact]
    public void EncryptDecryptBytesRoundTrips() {
        byte[] plainBytes = new byte[4096];
        for (int index = 0; index < plainBytes.Length; index++)
            plainBytes[index] = (byte)(index % 251);

        byte[] envelope = MessageCipher.EncryptBytes(plainBytes);
        byte[] decrypted = MessageCipher.DecryptBytes(envelope);

        Assert.False(envelope.SequenceEqual(plainBytes));
        Assert.True(decrypted.SequenceEqual(plainBytes));
    }

    [Fact]
    public void DecryptBytesNullReturnsNull() {
        Assert.Null(MessageCipher.DecryptBytes(null));
    }
}
