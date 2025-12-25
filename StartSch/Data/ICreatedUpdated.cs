namespace StartSch.Data;

public interface ICreatedUpdated
{
    Instant Created { get; set; }
    Instant Updated { get; set; }
}
