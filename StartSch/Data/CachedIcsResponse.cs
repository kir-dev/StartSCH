using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

/// <summary>
/// Caches the raw external calendar (.ics) responses fetched from Neptun/Moodle so that a
/// temporary upstream outage does not cause StartSCH to serve an empty calendar to Google
/// (which Google interprets as event deletions).
///
/// Rows are keyed by a hash of the external calendar URL — not per user — so users subscribed
/// to the same calendar share a single row (cross-user dedup). The payload is the raw .ics text,
/// deflate-compressed, then AES-256-GCM-encrypted under a key derived from the URL
/// (HKDF-SHA256, domain-separated from the lookup hash). A database leak alone therefore does
/// not reveal calendar contents, and no DP-protected user token is ever stored here.
/// </summary>
[Index(nameof(UrlHash), IsUnique = true)]
public class CachedIcsResponse
{
    public int Id { get; set; }

    /// <summary>Domain-separated HKDF-derived lookup key for the external calendar URL. Unique.</summary>
    public byte[] UrlHash { get; set; } = null!;

    /// <summary>Instant of the last successful upstream fetch; entries older than 48h are treated as stale.</summary>
    public Instant UpdatedAt { get; set; }

    /// <summary>raw .ics (UTF-8) |&gt; deflate-compress() |&gt; AES-256-GCM(URL-derived key).</summary>
    public byte[] Data { get; set; } = null!;

    public byte[] Nonce { get; set; } = null!;
    public byte[] Tag { get; set; } = null!;

    /// <summary>Key-derivation scheme version (allows future rotation without losing existing rows).</summary>
    public int Version { get; set; }
}
