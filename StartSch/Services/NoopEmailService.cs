namespace StartSch.Services;

public class NoopEmailService : IEmailService
{
    private readonly ILogger<NoopEmailService> _logger;

    public NoopEmailService(ILogger<NoopEmailService> logger)
    {
        _logger = logger;
        logger.LogWarning("No email service configured, using noop service.");
    }

    public Task Send(string to, string subject, string html, string? replyTo = null)
    {
        _logger.LogInformation("Sending nothing to 1 recipient.");
        return Task.CompletedTask;
    }
}