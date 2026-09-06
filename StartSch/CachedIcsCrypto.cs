using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace StartSch;

/// <summary>
/// Encrypt/decrypt helpers for <see cref="Data.CachedIcsResponse"/>.
///
/// The encryption key and the cache-lookup hash are both derived from the external calendar URL via
/// HKDF-SHA256 with domain-separated, versioned <c>info</c> labels, so they are independent even
/// though they share the same input. The lookup hash is the only thing stored in the database; the
/// actual AES key is never persisted and can only be recomputed from the URL. Because the URL is a
/// full-entropy bearer secret, no password-style key stretching is needed.
///
/// Payload layout: plaintext |&gt; deflate-compress() |&gt; AES-256-GCM(URL-derived key).
/// AES-GCM requires a fresh random nonce per encryption (the derived key is deterministic).
/// </summary>
public static class CachedIcsCrypto
{
    public const int CurrentSchemeVersion = 1;

    private const int KeySizeBytes = 32; // AES-256
    private const int LookupHashSizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private static readonly Encoding Text = Encoding.UTF8;

    public static byte[] DeriveLookupHash(string url, int version = CurrentSchemeVersion)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, Text.GetBytes(url), LookupHashSizeBytes,
            Array.Empty<byte>(), GetInfo("lookup", version));

    /// <summary>Compress, then AES-256-GCM-encrypt the raw .ics text. Returns ciphertext, nonce and tag.</summary>
    public static (byte[] data, byte[] nonce, byte[] tag) Encrypt(string url, string ics,
        int version = CurrentSchemeVersion)
    {
        byte[] plaintext = Compress(Text.GetBytes(ics));
        byte[] key = DeriveKey(url, version);
        byte[] nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSizeBytes];
        using (var aes = new AesGcm(key, TagSizeBytes))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, nonce, tag);
    }

    /// <summary>Decrypt, then decompress a stored payload back to the raw .ics text.</summary>
    public static string Decrypt(string url, byte[] data, byte[] nonce, byte[] tag,
        int version = CurrentSchemeVersion)
    {
        byte[] key = DeriveKey(url, version);
        byte[] plaintext = new byte[data.Length];
        using (var aes = new AesGcm(key, TagSizeBytes))
            aes.Decrypt(nonce, data, tag, plaintext);
        return Text.GetString(Decompress(plaintext));
    }

    private static byte[] DeriveKey(string url, int version)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, Text.GetBytes(url), KeySizeBytes,
            Array.Empty<byte>(), GetInfo("encryption", version));

    private static byte[] GetInfo(string purpose, int version)
        => Text.GetBytes($"StartSch.CachedIcsResponse:v{version}:{purpose}");

    private static byte[] Compress(byte[] input)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(input, 0, input.Length);
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var deflate = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }
}
