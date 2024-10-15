namespace StartSch.Wasm;

public class SelectableTagGroup<TData>(
    string id,
    TData? data = default,
    List<SelectableTagGroup<TData>>? children = null)
    : TagGroup<SelectableTagGroup<TData>, TData>(id, data, children), IConstructFromTagGroup<SelectableTagGroup<TData>, TagGroup<>>
{
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
        else if (Children != null)
            foreach (var child in Children)
                child.SerializeSelection(results);
    }

    public static void DeserializeSelection(List<SelectableTagGroup<TData>> groups, List<string> selectedTags)
    {
        foreach (string tag in selectedTags)
        {
            Queue<string> segments = new(tag.Split('.'));
            IReadOnlyList<SelectableTagGroup<TData>> candidates = groups;
            while (segments.Count != 0)
            {
                string groupId = segments.Dequeue();
                SelectableTagGroup<TData>? group = candidates.FirstOrDefault(g => g.Id == groupId);
                if (group == null)
                    break;
                if (segments.Count == 0 && !group.IsSelected)
                    group.Toggle();
                if (group.Children == null)
                    break;
                candidates = group.Children;
            }
        }
    }

    public void Toggle()
    {
        IsSelected = !IsSelected;
        if (IsSelected)
        {
            Parent?.OnChildSelected();
            if (Children != null)
                foreach (var child in Children)
                    child.OnParentSelected();
        }
        else
        {
            Parent?.OnChildUnselected();
            if (Children != null)
                foreach (var child in Children)
                    child.OnParentUnselected();
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
        if (Children != null)
            foreach (var child in Children)
                child.OnParentSelected();
    }

    private void OnParentUnselected()
    {
        IsSelected = false;
        if (Children != null)
            foreach (var child in Children)
                child.OnParentSelected();
    }

    public static SelectableTagGroup<TData> Copy(SelectableTagGroup<TData> source)
        => new(source.Id, source.Data);

    public static SelectableTagGroup<TData> ConstructFrom<TSource>(TSource source) where TSource : TagGroup<TSource, TData>, IConstructFromTagGroup<TSource, TData>
    {
        return new(source.Id, source.Data);
    }
}