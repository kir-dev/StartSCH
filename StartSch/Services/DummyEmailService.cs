namespace StartSch.Services;

public class DummyEmailService : IEmailService
{
    private readonly ILogger<DummyEmailService> _logger;

    public DummyEmailService(ILogger<DummyEmailService> logger)
    {
        _logger = logger;
        logger.LogWarning("No email service configured, using dummy service.");
    }

    public Task Send(IEnumerable<string> to, string subject, string html, string? replyTo = null)
    {
        _logger.LogInformation("Sending nothing to {Count} recipients.", to.Count());
        return Task.CompletedTask;
    }
}