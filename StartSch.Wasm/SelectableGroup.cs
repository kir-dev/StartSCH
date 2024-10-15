namespace StartSch.Wasm;

public class SelectableGroup<T>
{
    public SelectableGroup(string id, T? data = default, List<SelectableGroup<T>>? children = null)
    {
        if (id.Contains('.'))
            throw new ArgumentException($"Invalid {nameof(SelectableGroup<T>)} id");
        Id = id;
        Data = data;
        _children = children;
        _children?.ForEach(c => c.Parent = this);
    }

    public string Id { get; }
    public T? Data { get; set; }
    private List<SelectableGroup<T>>? _children;
    public IReadOnlyList<SelectableGroup<T>>? Children => _children;
    public SelectableGroup<T>? Parent { get; private set; }
    public bool IsSelected { get; private set; }

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

    public static void DeserializeSelection(List<SelectableGroup<T>> groups, List<string> selectedTags)
    {
        foreach (string tag in selectedTags)
        {
            Queue<string> segments = new(tag.Split('.'));
            List<SelectableGroup<T>> candidates = groups;
            while (segments.Count != 0)
            {
                string groupId = segments.Dequeue();
                SelectableGroup<T>? group = candidates.Find(g => g.Id == groupId);
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
    public static List<SelectableGroup<T>> Merge(IEnumerable<SelectableGroup<T>> groups)
    {
        Dictionary<string, SelectableGroup<T>> map = [];
        HashSet<SelectableGroup<T>> seen = [];
        List<SelectableGroup<T>> results = [];

        foreach (var group in groups)
            Add(group);

        return results;

        void Add(SelectableGroup<T> node, SelectableGroup<T>? parentEntry = null)
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