using Shared.DI.ProvidersConfig.Lite.Abstractions;
using Shared.DI.ProvidersConfig.Lite.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Lite.Example.Services;

public class MessageSender : IMessageSender, IHasProviders<IMessageProvider>
{
    private readonly IProviders<MessageSender, IMessageProvider> _providers;
    private readonly IProviderSwitcher<MessageSender, IMessageProvider> _switcher;

    public MessageSender(
        IProviders<MessageSender, IMessageProvider> providers,
        IProviderSwitcher<MessageSender, IMessageProvider> switcher)
    {
        _providers = providers;
        _switcher = switcher;
    }

    public async Task SendEmailAsync(string message)
    {
        var emailProvider = _providers.Of("email");
        await emailProvider.SendAsync(message);
    }

    public async Task SendSmsAsync(string message)
    {
        var smsProvider = _providers.Of("sms");
        await smsProvider.SendAsync(message);
    }

    public async Task SendBackupSmsAsync(string message)
    {
        var backupSmsProvider = _providers.Of("backup-sms");
        await backupSmsProvider.SendAsync(message);
    }

    public async Task SendUsingDefaultAsync(string message)
    {
        var defaultProvider = _providers.Provider;
        await defaultProvider.SendAsync(message);
    }

    public void SwitchToEmail()
    {
        _switcher.CurrentKey = "email";
    }

    public void SwitchToSms()
    {
        _switcher.CurrentKey = "sms";
    }

    public void SwitchToBackupSms()
    {
        _switcher.CurrentKey = "backup-sms";
    }
}
