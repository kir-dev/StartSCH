namespace StartSch.Wasm;

public class TagGroup(
    string id,
    string? description = null,
    List<TagGroup>? children = null)
    : SelectableGroup<TagGroup>(children)
{
    public string Id { get; } = id;
    public string? Description { get; } = description;

    public override string ToString() => Parent == null ? Id : $"{Parent}.{Id}";
}

public abstract class SelectableGroup<T> where T : SelectableGroup<T>
{
    protected SelectableGroup(List<T>? children)
    {
        Children = children;
        Children?.ForEach(c => c.Parent = (T)this);
    }

    public List<T>? Children { get; }
    protected T? Parent { get; private set; }
    public bool IsSelected { get; private set; }

    public void Toggle()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
        {
            Parent?.OnChildSelected();
            Children?.ForEach(c => c.OnParentSelected());
        }
        else
        {
            Parent?.OnChildUnselected();
            Children?.ForEach(c => c.OnParentUnselected());
        }
    }

    private void OnChildSelected()
    {
        if (Children == null) throw new InvalidOperationException();

        if (Children.All(c => c.IsSelected))
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
        Children?.ForEach(c => c.OnParentSelected());
    }

    private void OnParentUnselected()
    {
        IsSelected = false;
        Children?.ForEach(c => c.OnParentUnselected());
    }
}