using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig;
using Shared.DI.ProvidersConfig.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Example.Providers;

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
        Console.WriteLine($"[SecondarySmsProvider] Provider: {_options.Value.ProviderName}");
        Console.WriteLine($"[SecondarySmsProvider] Sending via API {_options.Value.ApiUrl}");
        Console.WriteLine($"[SecondarySmsProvider] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[SecondarySmsProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
