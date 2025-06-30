namespace StartSch;

public class ModuleUnavailableException : Exception
{
    public ModuleUnavailableException()
    {
    }

    public ModuleUnavailableException(Exception innerException) : base(null, innerException)
    {
    }
}
