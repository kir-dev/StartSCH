namespace StartSch.Services;

public interface IEmailService
{
    Task Send(SingleSendRequestDto request);
    Task Send(MultipleSendRequestDto request);
}