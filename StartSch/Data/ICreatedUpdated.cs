namespace StartSch.Data;

/// Created and updated times are automatically set when calling Db.SaveChangesAsync()
public interface IAutoCreatedUpdated
{
    DateTime Created { get; set; }
    DateTime Updated { get; set; }
}
