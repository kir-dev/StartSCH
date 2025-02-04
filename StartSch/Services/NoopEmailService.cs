namespace StartSch.Services;

public class NoopEmailService : IEmailService
{
    private readonly ILogger<NoopEmailService> _logger;

    public NoopEmailService(ILogger<NoopEmailService> logger)
    {
        _logger = logger;
        logger.LogWarning("No email service configured, using no-op service.");
    }

    public Task Send(string from, string to, string subject, string html)
    {
        _logger.LogInformation("Not sending email to 1 recipient.");
        return Task.CompletedTask;
    }

    public Task Send(string from, List<string> to, string subject, string html)
    {
        _logger.LogInformation("Not sending email to {Count} recipients.", to.Count);
        return Task.CompletedTask;
    }
}