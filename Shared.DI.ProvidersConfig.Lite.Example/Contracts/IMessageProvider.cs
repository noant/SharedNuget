namespace Shared.DI.ProvidersConfig.Lite.Example.Contracts;

public interface IMessageProvider
{
    Task SendAsync(string message);
}
