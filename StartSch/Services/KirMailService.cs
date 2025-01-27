namespace StartSch.Services;

public class KirMailService(IHttpClientFactory httpClientFactory, ILogger<KirMailService> logger) : IEmailService
{
    public async Task Send(string to, string subject, string html, string? replyTo = null)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        var request = new SingleSendRequestDto(
            new("StartSCH", null),
            to,
            subject,
            html,
            replyTo);
        logger.LogInformation("Sending email to 1 recipient.");
        await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send", request);
    }

    private record SingleSendRequestDto(
        FromDto From,
        string To,
        string Subject,
        string Html,
        string? ReplyTo
    );

    private record FromDto(
        string Name,
        string? Email // everything just gets sent from noreply@m1.kir-dev.hu
    );
}