using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(UrlHash), IsUnique = true)]
public class CachedIcsResponse
{
    public int Id { get; set; }

    public byte[] UrlHash { get; set; } = null!;

    public Instant UpdatedAt { get; set; }

    /// .ics > compress() > encrypt(KDF(URL)) > store
    public byte[] Data { get; set; } = null!;

    public byte[] Nonce { get; set; } = null!;
    public byte[] Tag { get; set; } = null!;
}
