using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig;
using Shared.DI.ProvidersConfig.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Example.Providers;

public class EmailProviderOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
}

public class EmailProvider : IMessageProvider, IProvider<IMessageProvider, EmailProviderOptions>
{
    private readonly EmailProviderOptions _options;

    public EmailProvider(IOptions<EmailProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[EmailProvider] Sending via SMTP {_options.SmtpHost}:{_options.SmtpPort}");
        Console.WriteLine($"[EmailProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
