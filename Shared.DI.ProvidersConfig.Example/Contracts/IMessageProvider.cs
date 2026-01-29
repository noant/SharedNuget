namespace Shared.DI.ProvidersConfig.Example.Contracts;

public interface IMessageProvider
{
    Task SendAsync(string message);
}
