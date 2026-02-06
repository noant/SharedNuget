using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig.Lite.Abstractions;
using Shared.DI.ProvidersConfig.Lite.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Lite.Example.Providers;

public class SecondarySmsProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
}

public class SecondarySmsProvider : IMessageProvider, IProvider<IMessageProvider, SecondarySmsProviderOptions>
{
    private readonly IOptionsSnapshot<SecondarySmsProviderOptions> _options;

    public SecondarySmsProvider(IOptionsSnapshot<SecondarySmsProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[{_options.Value.ProviderName}] Sending via API {_options.Value.ApiUrl}");
        Console.WriteLine($"[{_options.Value.ProviderName}] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[{_options.Value.ProviderName}] Message: {message}");
        return Task.CompletedTask;
    }
}
