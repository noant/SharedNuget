using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig.Lite.Abstractions;
using Shared.DI.ProvidersConfig.Lite.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Lite.Example.Providers;

public class SmsProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}

public class SmsProvider : IMessageProvider, IProvider<IMessageProvider, SmsProviderOptions>
{
    private readonly IOptionsSnapshot<SmsProviderOptions> _options;

    public SmsProvider(IOptionsSnapshot<SmsProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[SmsProvider] Sending via API {_options.Value.ApiUrl}");
        Console.WriteLine($"[SmsProvider] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[SmsProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
