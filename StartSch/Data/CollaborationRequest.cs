namespace StartSch.Data;

public abstract class CollaborationRequest
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;
}

public class EventCollaborationRequest : CollaborationRequest
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}

public class PostCollaborationRequest : CollaborationRequest
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}
