namespace StartSch;

public record Tag
{
    public Tag(string Id,
        string? Description = null,
        List<Tag>? Children = null)
    {
        this.Id = Id;
        this.Description = Description;
        this.Children = Children;

        if (Children != null)
            foreach (var child in Children)
                child.Parent = this;
    }

    public string Id { get; }
    public string? Description { get; }
    public List<Tag>? Children { get; }
    public Tag? Parent { get; set; }

    public override string ToString() => Parent == null ? Id : $"{Parent}.{Id}";
}