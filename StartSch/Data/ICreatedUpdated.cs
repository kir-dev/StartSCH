namespace StartSch.Data;

/// Created and updated times are automatically set when calling Db.SaveChangesAsync()
public interface IAutoCreatedUpdated
{
    Instant Created { get; set; }
    Instant Updated { get; set; }
}
