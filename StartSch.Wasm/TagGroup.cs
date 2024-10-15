namespace StartSch.Wasm;

public class TagGroup<TData>(
    string id,
    TData? data = default,
    List<TagGroup<TData>>? children = null)
    : TagGroup<TagGroup<TData>, TData>(id, data, children), IConstructFromTagGroup<TagGroup<TData>, TData>
{
    public static TagGroup<TData> ConstructFrom<TSource>(TSource source) where TSource : TagGroup<TSource, TData>, IConstructFromTagGroup<TSource, TData>
        => new(source.Id, source.Data);
}

public class TagGroup<TThis, TData>
    where TThis : TagGroup<TThis, TData>, IConstructFromTagGroup<TThis, TData>
{
    public TagGroup(string id, TData? data, List<TThis>? children)
    {
        if (id.Contains('.'))
            throw new ArgumentException($"Invalid {nameof(TThis)} id");
        Id = id;
        Data = data;
        _children = children;
        _children?.ForEach(c => c.Parent = (TThis?)this);
    }

    private List<TThis>? _children;
    public string Id { get; }
    public TData? Data { get; set; }
    public IReadOnlyList<TThis>? Children => _children;
    protected TThis? Parent { get; private set; }

    /// <param name="groups">Arbitrary list of groups</param>
    /// <returns>A newly allocated list of unselected groups</returns>
    public static List<TThis> Merge<TSource>(IEnumerable<TSource> groups)
        where TSource : TagGroup<TSource, TData>, IConstructFromTagGroup<TSource, TData>
    {
        Dictionary<string, TThis> map = [];
        HashSet<TSource> seen = [];
        List<TThis> results = [];

        foreach (var group in groups)
            Add(group);

        return results;

        void Add(TSource node, TThis? parentEntry = null)
        {
            string path = node.ToString();
            if (!seen.Add(node)) return;

            if (map.TryGetValue(path, out var entry))
            {
                // use the non-empty details that were found first
                if (node.Data != null && entry.Data == null)
                    entry.Data = node.Data;
            }
            else
            {
                map[path] = entry = TThis.ConstructFrom(node);
                if (node.Parent == null)
                    results.Add(entry);
                else
                {
                    (parentEntry!._children ??= []).Add(entry);
                    entry.Parent = parentEntry;
                }
            }

            if (node.Children != null)
                foreach (var child in node.Children)
                    Add(child, entry);
        }
    }

    public sealed override string ToString() => Parent == null ? Id : $"{Parent}.{Id}";
}