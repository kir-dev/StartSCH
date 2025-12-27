namespace StartSch.Data;

[Flags]
public enum TimestampUpdateFlags
{
    None = 0,
    Created = 1,
    Updated = 2,
    CreatedUpdated = Created | Updated,
}
