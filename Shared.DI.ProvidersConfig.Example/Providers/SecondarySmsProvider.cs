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
    private readonly SecondarySmsProviderOptions _options;

    public SecondarySmsProvider(IOptions<SecondarySmsProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[SecondarySmsProvider] Provider: {_options.ProviderName}");
        Console.WriteLine($"[SecondarySmsProvider] Sending via API {_options.ApiUrl}");
        Console.WriteLine($"[SecondarySmsProvider] API Key: {_options.ApiKey}");
        Console.WriteLine($"[SecondarySmsProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
