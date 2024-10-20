namespace StartSch.Services;

public class KirMailService(IHttpClientFactory httpClientFactory, ILogger<KirMailService> logger) : IEmailService
{
    public async Task Send(IEnumerable<string> to, string subject, string html, string? replyTo = null)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        var toList = to.ToList();
        var request = new MultipleSendRequestDto(
            new("StartSch", null),
            toList,
            subject,
            html,
            replyTo);
        logger.LogInformation("Sending email to {Count} recipients.", toList.Count);
        await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send-to-many", request);
    }

    private record MultipleSendRequestDto(
        FromDto From,
        IEnumerable<string> To,
        string Subject,
        string Html,
        string? ReplyTo
    );

    private record FromDto(
        string Name,
        string? Email // everything just gets sent from noreply@m1.kir-dev.hu
    );
}