namespace StartSch.Services;

public class KirMailService(IHttpClientFactory httpClientFactory, ILogger<KirMailService> logger) : IEmailService
{
    public async Task Send(SingleSendRequestDto request)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        logger.LogInformation("Sending email to 1 recipient.");
        await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send", request);
    }

    public async Task Send(MultipleSendRequestDto request)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        logger.LogInformation("Sending email to {Count} recipients.", request.To.Count);
        await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send-to-many", request);
    }
}
