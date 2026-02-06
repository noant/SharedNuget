using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig.Lite.Abstractions;
using Shared.DI.ProvidersConfig.Lite.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Lite.Example.Providers;

public class EmailProviderOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
}

public class EmailProvider : IMessageProvider, IProvider<IMessageProvider, EmailProviderOptions>
{
    private readonly IOptionsSnapshot<EmailProviderOptions> _options;

    public EmailProvider(IOptionsSnapshot<EmailProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[EmailProvider] Sending via SMTP {_options.Value.SmtpHost}:{_options.Value.SmtpPort}");
        Console.WriteLine($"[EmailProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
