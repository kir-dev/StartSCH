namespace StartSch.Services;

public interface IEmailService
{
    Task Send(string from, string to, string subject, string html);
    Task Send(string from, List<string> to, string subject, string html);
}