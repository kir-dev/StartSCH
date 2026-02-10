namespace StartSch.Data;

public interface IEventNode
{
    List<Category> Categories { get; }
    Event? Parent { get; }
}
