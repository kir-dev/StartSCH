namespace StartSch.Data;

// A Post or an Event
public interface IArticle
{
    List<Category> Categories { get; }
    Event? Parent { get; }
}
