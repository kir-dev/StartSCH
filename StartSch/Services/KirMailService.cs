namespace StartSch.Services;

public class KirMailService(IHttpClientFactory httpClientFactory, ILogger<KirMailService> logger) : IEmailService
{
    public async Task Send(SingleSendRequestDto request)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        logger.LogInformation("Sending email to 1 recipient.");
        var responseMessage = await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send", request);
        responseMessage.EnsureSuccessStatusCode();
    }

    public async Task Send(MultipleSendRequestDto request)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        logger.LogInformation("Sending email to {Count} recipients.", request.To.Count);
        var responseMessage = await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send-to-many", request);
        responseMessage.EnsureSuccessStatusCode();
    }
}
