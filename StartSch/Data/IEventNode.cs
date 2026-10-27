namespace StartSch.Data;

// A Post or an Event
public interface IEventNode
{
    List<Category> Categories { get; }
    Event? Parent { get; }
}
