using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;

namespace StartSch.Data;

public abstract class PersonalCalendar
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [MaxLength(200)] public string Name { get; set; } = null!;
    public User User { get; set; } = null!;
}

public abstract class ExternalPersonalCalendar : PersonalCalendar
{
    private string? _urlCache;

    [UsedImplicitly] public byte[]? AesNonce { get; set; }

    [UsedImplicitly]
    public byte[]? AesEncryptedUrl
    {
        get;
        set
        {
            field = value;
            _urlCache = null;
        }
    } = null!;

    [UsedImplicitly] public byte[]? AesTag { get; set; }

    public void SetUrl(string url, ReadOnlySpan<byte> aesKey)
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
        if (AesEncryptedUrl is null)
            return null;

        if (_urlCache != null)
            return _urlCache;
        
        using AesGcm aesGcm = new(aesKey, tagSizeInBytes: 16);
        byte[] decryptedBytes = new byte[AesEncryptedUrl.Length];
        aesGcm.Decrypt(AesNonce!, AesEncryptedUrl, AesTag!, decryptedBytes);
        _urlCache = Encoding.UTF8.GetString(decryptedBytes);

        return _urlCache;
    }

    public void Clear()
    {
        AesNonce = null;
        AesEncryptedUrl = null;
        AesTag = null;
        _urlCache = null;
    }
}

public class PersonalMoodleCalendar : ExternalPersonalCalendar;

public class PersonalNeptunCalendar : ExternalPersonalCalendar;

// TODO: rename to PersonalCalendarCategory
public class PersonalStartSchCalendar : PersonalCalendar
{
    public List<Event> Events { get; set; } = null!;
}
