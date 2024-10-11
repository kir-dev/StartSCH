namespace StartSch;

public record Tag(string Id, string? Description = null, List<Tag>? Children = null);