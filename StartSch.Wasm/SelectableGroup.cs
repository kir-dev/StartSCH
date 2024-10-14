namespace StartSch.Wasm;

public abstract class SelectableGroup<TGroup> where TGroup : SelectableGroup<TGroup>
{
    protected SelectableGroup(string id, List<TGroup>? children)
    {
        Id = id;
        _children = children;
        _children?.ForEach(c => c.Parent = (TGroup)this);
    }

    public string Id { get; }
    private readonly List<TGroup>? _children;
    public IReadOnlyList<TGroup>? Children => _children;
    public TGroup? Parent { get; private set; }
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
            results.Add(ToString()!);
        else
            _children?.ForEach(c => c.SerializeSelection(results));
    }

    public static void DeserializeSelection(List<TGroup> groups, List<string> selectedTags)
    {
        foreach (string tag in selectedTags)
        {
            Queue<string> segments = new(tag.Split('.'));
            List<TGroup> candidates = groups;
            while (segments.Count != 0)
            {
                string groupId = segments.Dequeue();
                TGroup? group = candidates.Find(g => g.Id == groupId);
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