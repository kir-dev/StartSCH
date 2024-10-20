namespace StartSch.Services;

public interface IEmailService
{
    Task Send(IEnumerable<string> to, string subject, string html, string? replyTo = null);
}