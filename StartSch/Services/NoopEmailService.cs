namespace StartSch.Services;

public class NoopEmailService : IEmailService
{
    private readonly ILogger<NoopEmailService> _logger;

    public NoopEmailService(ILogger<NoopEmailService> logger)
    {
        _logger = logger;
        logger.LogWarning("No email service configured, using no-op service.");
    }

    public Task Send(SingleSendRequestDto request)
    {
        _logger.LogInformation("Not sending email to 1 recipient.");
        return Task.CompletedTask;
    }

    public Task Send(MultipleSendRequestDto request)
    {
        _logger.LogInformation("Not sending email to {Count} recipients.", request.To.Count);
        return Task.CompletedTask;
    }
}