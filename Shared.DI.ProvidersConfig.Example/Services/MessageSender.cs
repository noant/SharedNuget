using Shared.DI.ProvidersConfig;
using Shared.DI.ProvidersConfig.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Example.Services;

public class MessageSender : IMessageSender, IHasProviders<MessageType, IMessageProvider>
{
    private readonly IProviders<MessageType, IMessageProvider> _providers;

    public MessageSender(IProviders<MessageType, IMessageProvider> providers)
    {
        _providers = providers;
    }

    public async Task SendEmailAsync(string message)
    {
        var emailProvider = _providers.Of(MessageType.Email);
        await emailProvider.SendAsync(message);
    }

    public async Task SendSmsAsync(string message)
    {
        var smsProvider = _providers.Of(MessageType.Sms);
        await smsProvider.SendAsync(message);
    }

    public async Task SendUsingDefaultAsync(string message)
    {
        var defaultProvider = _providers.Provider;
        await defaultProvider.SendAsync(message);
    }
}
