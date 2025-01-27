namespace StartSch.Services;

public interface IEmailService
{
    Task Send(string to, string subject, string html, string? replyTo = null);
}