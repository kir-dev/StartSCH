using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace StartSch;

public static class CachedIcsCrypto
{
    private const int KeySizeBytes = 32; // AES-256
    private const int LookupHashSizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public static byte[] DeriveLookupHash(string url)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(url), LookupHashSizeBytes, [], GetInfo("lookup"));

    public static (byte[] data, byte[] nonce, byte[] tag) Encrypt(string url, string ics)
    {
        byte[] plaintext = Compress(Encoding.UTF8.GetBytes(ics));
        byte[] key = DeriveKey(url);
        byte[] nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSizeBytes];
        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, nonce, tag);
    }

    public static string Decrypt(string url, byte[] data, byte[] nonce, byte[] tag)
    {
        byte[] key = DeriveKey(url);
        byte[] plaintext = new byte[data.Length];
        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, data, tag, plaintext);
        return Encoding.UTF8.GetString(Decompress(plaintext));
    }

    private static byte[] DeriveKey(string url)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(url), KeySizeBytes, [], GetInfo("encryption"));

    private static byte[] GetInfo(string purpose)
        => Encoding.UTF8.GetBytes($"StartSch.CachedIcsResponse:{purpose}");

    private static byte[] Compress(byte[] uncompressedData)
    {
        using var compressedStream = new MemoryStream();
        using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(uncompressedData);
        return compressedStream.ToArray();
    }

    private static byte[] Decompress(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var deflate = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        deflate.CopyTo(decompressedStream);
        return decompressedStream.ToArray();
    }
}
