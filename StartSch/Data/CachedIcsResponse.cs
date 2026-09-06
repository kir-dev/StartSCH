namespace StartSch.Data;

public class CachedIcsResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public Instant Time { get; set; }
    public byte[] UrlHash { get; set; } = null!;

    // plaintext |> compress() |> encrypt(user's encryption key)
    public byte[] Data { get; set; } = null!;

    public User User { get; set; } = null!;
}
