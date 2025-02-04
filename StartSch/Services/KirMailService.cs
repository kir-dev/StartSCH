using JetBrains.Annotations;

namespace StartSch.Services;

public class KirMailService(IHttpClientFactory httpClientFactory, ILogger<KirMailService> logger) : IEmailService
{
    public async Task Send(string from, string to, string subject, string html)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        SingleSendRequestDto request = new(new(from, null), to, subject, html);
        logger.LogInformation("Sending email to 1 recipient.");
        await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send", request);
    }

    public async Task Send(string from, List<string> to, string subject, string html)
    {
        HttpClient client = httpClientFactory.CreateClient(nameof(KirMailService));
        MultipleSendRequestDto request = new(new(from, null), to, subject, html);
        logger.LogInformation("Sending email to {Count} recipients.", request.To.Count);
        await client.PostAsJsonAsync("https://mail.kir-dev.hu/api/send-to-many", request);
    }

    [UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
    private record SingleSendRequestDto(
        FromDto From,
        string To,
        string Subject,
        string Html,
        string? ReplyTo = null,
        string? Queue = null
    );

    [UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
    private record MultipleSendRequestDto(
        FromDto From,
        List<string> To,
        string Subject,
        string Html,
        string? ReplyTo = null,
        string? Queue = null
    );

    [UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
    private record FromDto(
        string Name,
        string? Email // everything just gets sent from noreply@m1.kir-dev.hu
    );
}