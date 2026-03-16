using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace StartSch.Data;

public class PersonalCalendar
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required User User { get; set; }
}

public class ExternalPersonalCalendar : PersonalCalendar
{
    public byte[] RsaEncryptedAesKey { get; set; } = null!;
    public byte[] AesNonce { get; set; } = null!;
    public byte[] AesEncryptedUrl { get; set; } = null!;
    public byte[] AesTag { get; set; } = null!;

    public void SetUrl(string url)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(User.PublicEncryptionKey.AsSpan(), out _);

        byte[] aesKey = new byte[32];
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(aesKey);
        RandomNumberGenerator.Fill(nonce);

        RsaEncryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
        AesNonce = nonce;

        byte[] urlBytes = Encoding.UTF8.GetBytes(url);
        byte[] ciphertext = new byte[urlBytes.Length];
        byte[] tag = new byte[16];

        using var aesGcm = new AesGcm(aesKey, tagSizeInBytes: 16);
        aesGcm.Encrypt(nonce, urlBytes, ciphertext, tag);

        AesEncryptedUrl = ciphertext;
        AesTag = tag;
    }

    public string DecryptUrl(byte[] privateKey)
    {
        using var rsa = RSA.Create();
        
        // Note: ImportFromPem is available in .NET 5+. 
        // If your string is a raw Base64 DER, use rsa.ImportPkcs8PrivateKey() instead.
        rsa.ImportRSAPrivateKey(privateKey, out _);

        // 2. Decrypt the AES key using the same secure OAEP padding
        byte[] aesKey = rsa.Decrypt(RsaEncryptedAesKey, RSAEncryptionPadding.OaepSHA256);

        try
        {
            // 3. Initialize AES-GCM with the decrypted symmetric key
            using var aesGcm = new AesGcm(aesKey, tagSizeInBytes: 16);

            // 4. Prepare a buffer for the decrypted plaintext
            byte[] decryptedBytes = new byte[AesEncryptedUrl.Length];

            // 5. Decrypt and Authenticate
            // If the URL, Nonce, or Tag has been tampered with, this method will 
            // automatically throw a CryptographicException.
            aesGcm.Decrypt(AesNonce, AesEncryptedUrl, AesTag, decryptedBytes);

            // 6. Convert the decoded bytes back into the URL string
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        finally
        {
            // Security Best Practice: Zero out the symmetric key in memory
            // as soon as we are done with it so it can't be scraped from RAM.
            CryptographicOperations.ZeroMemory(aesKey);
        }
    }
}

public class PersonalMoodleCalendar : ExternalPersonalCalendar;

public class PersonalNeptunCalendar : ExternalPersonalCalendar;

public class PersonalStartSchCalendar : PersonalCalendar
{
    public List<Event> Events { get; set; }
}

public class PersonalCalendarExport
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
}


