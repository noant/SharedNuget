namespace Shared.DI.ProvidersConfig.Example.Contracts;

public interface IMessageSender
{
    Task SendEmailAsync(string message);
    Task SendSmsAsync(string message);
    Task SendUsingDefaultAsync(string message);
}
