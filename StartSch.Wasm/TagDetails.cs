namespace StartSch.Wasm;

public record TagDetails(string? Description)
{
    public static implicit operator TagDetails(string s) => new(s);
}