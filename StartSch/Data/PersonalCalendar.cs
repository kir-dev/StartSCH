using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace StartSch.Data;

public class PersonalCalendar
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [MaxLength(200)] public required string Name { get; set; }
    public required User User { get; set; }
}

public abstract class ExternalPersonalCalendar : PersonalCalendar
{
    private string? _urlCache;

    public byte[] AesNonce { get; set; } = null!;

    public byte[] AesEncryptedUrl
    {
        get;
        set
        {
            field = value;
            _urlCache = null;
        }
    } = null!;

    public byte[] AesTag { get; set; } = null!;

    public void SetUrl(string url, byte[] aesKey)
    {
        if (url == _urlCache) return;
        if (aesKey.Length != 32) throw new ArgumentException("", nameof(aesKey));
        
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        byte[] urlBytes = Encoding.UTF8.GetBytes(url);
        byte[] ciphertext = new byte[urlBytes.Length];
        byte[] tag = new byte[16];

        using var aesGcm = new AesGcm(aesKey, tagSizeInBytes: 16);
        aesGcm.Encrypt(nonce, urlBytes, ciphertext, tag);

        AesNonce = nonce;
        AesEncryptedUrl = ciphertext;
        AesTag = tag;
        _urlCache = url;
    }

    public string? GetUrl(byte[] aesKey)
    {
        if (_urlCache == null)
        {
            using AesGcm aesGcm = new(aesKey, tagSizeInBytes: 16);
            byte[] decryptedBytes = new byte[AesEncryptedUrl.Length];
            aesGcm.Decrypt(AesNonce, AesEncryptedUrl, AesTag, decryptedBytes);
            _urlCache = Encoding.UTF8.GetString(decryptedBytes);
        }

        return _urlCache;
    }
}

public class PersonalMoodleCalendar : ExternalPersonalCalendar;

public class PersonalNeptunCalendar : ExternalPersonalCalendar;

public class PersonalStartSchCalendar : PersonalCalendar
{
    public List<Event> Events { get; set; } = null!;
}
