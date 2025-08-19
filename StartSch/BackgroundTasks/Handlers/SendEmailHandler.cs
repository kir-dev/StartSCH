using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.BackgroundTasks.Handlers;

public class SendEmailHandler(IEmailService emailService, Db db)
    : IBackgroundTaskHandler<SendEmail>
{
    public async Task Handle(List<SendEmail> batch, CancellationToken cancellationToken)
    {
        var backgroundTasks = await db.BackgroundTasks
            .Where(x => batch.Contains(x))
            .Cast<SendEmail>()
            .Include(x => x.User)
            .ToListAsync(cancellationToken);
        List<int> messageIds = backgroundTasks
            .Select(x => x.MessageId)
            .Distinct()
            .ToList();
        await db.EmailMessages
            .Where(x => messageIds.Contains(x.Id))
            .LoadAsync(cancellationToken);
        await Task.WhenAll(
            backgroundTasks
                .GroupBy(x => x.Message)
                .Select(async group =>
                    {
                        EmailMessage message = group.Key;
                        await emailService.Send(new MultipleSendRequestDto(
                            new(message.FromName, "noreply@kir-dev.hu"),
                            group
                                .Select(x => x.User.GetVerifiedEmailAddress()!)
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .ToList(),
                            message.Subject,
                            message.ContentHtml,
                            null,
                            "ms-kirdev"
                        ));
                    }
                )
        );
    }
}
