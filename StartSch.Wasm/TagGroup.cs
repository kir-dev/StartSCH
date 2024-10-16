using System.Text.Json.Serialization;

namespace StartSch.Wasm;

public class TagGroup
{
    public TagGroup(string id, TagDetails? data = default, IReadOnlyList<TagGroup>? children = null)
    {
        if (id.Contains('.'))
            throw new ArgumentException($"Invalid {nameof(TagGroup)} id");
        Id = id;
        Data = data;
        _children = children?.ToList();
        _children?.ForEach(c => c.Parent = this);
    }

    public string Id { get; }
    public TagDetails? Data { get; set; }
    private List<TagGroup>? _children;
    public IReadOnlyList<TagGroup>? Children => _children;
    [JsonIgnore] public TagGroup? Parent { get; private set; }
    [JsonIgnore] public bool IsSelected { get; private set; }

    public List<string> SerializeSelection()
    {
        List<string> results = [];
        SerializeSelection(results);
        return results;
    }

    private void SerializeSelection(List<string> results)
    {
        if (IsSelected)
            results.Add(ToString());
        else
            _children?.ForEach(c => c.SerializeSelection(results));
    }

    public static void DeserializeSelection(List<TagGroup> groups, List<string> selectedTags)
    {
        foreach (string tag in selectedTags)
        {
            Queue<string> segments = new(tag.Split('.'));
            List<TagGroup> candidates = groups;
            while (segments.Count != 0)
            {
                string groupId = segments.Dequeue();
                TagGroup? group = candidates.Find(g => g.Id == groupId);
                if (group == null)
                    break;
                if (segments.Count == 0 && !group.IsSelected)
                    group.Toggle();
                if (group._children == null)
                    break;
                candidates = group._children;
            }
        }
    }

    /// <param name="groups">Arbitrary list of groups</param>
    /// <returns>A newly allocated list of unselected groups</returns>
    public static List<TagGroup> Merge(IEnumerable<TagGroup> groups)
    {
        Dictionary<string, TagGroup> map = [];
        HashSet<TagGroup> seen = [];
        List<TagGroup> results = [];

        foreach (var group in groups)
            Add(group);

        return results;

        void Add(TagGroup node, TagGroup? parentEntry = null)
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
                map[path] = entry = new(node.Id, node.Data);
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

    public void Toggle()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
        {
            Parent?.OnChildSelected();
            _children?.ForEach(c => c.OnParentSelected());
        }
        else
        {
            Parent?.OnChildUnselected();
            _children?.ForEach(c => c.OnParentUnselected());
        }
    }

    private void OnChildSelected()
    {
        if (_children == null) throw new InvalidOperationException();

        if (_children.All(c => c.IsSelected))
        {
            IsSelected = true;
            Parent?.OnChildSelected();
        }
    }

    private void OnChildUnselected()
    {
        IsSelected = false;
        Parent?.OnChildUnselected();
    }

    private void OnParentSelected()
    {
        IsSelected = true;
        _children?.ForEach(c => c.OnParentSelected());
    }

    private void OnParentUnselected()
    {
        IsSelected = false;
        _children?.ForEach(c => c.OnParentUnselected());
    }

    public sealed override string ToString() => Parent == null ? Id : $"{Parent}.{Id}";
}