namespace Shared.DI.ProvidersConfig.Lite.Example.Contracts;

public interface IMessageSender
{
    Task SendEmailAsync(string message);
    Task SendSmsAsync(string message);
    Task SendBackupSmsAsync(string message);
    Task SendUsingDefaultAsync(string message);
    void SwitchToEmail();
    void SwitchToSms();
    void SwitchToBackupSms();
}
