using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig;
using Shared.DI.ProvidersConfig.Example.Contracts;

namespace Shared.DI.ProvidersConfig.Example.Providers;

public class UniversalApiProviderOptions
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
}

public class UniversalApiProvider : IMessageProvider, IProvider<IMessageProvider, UniversalApiProviderOptions>
{
    private readonly IOptionsSnapshot<UniversalApiProviderOptions> _options;

    public UniversalApiProvider(IOptionsSnapshot<UniversalApiProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[UniversalApiProvider] Sending via Universal API {_options.Value.ApiUrl}");
        Console.WriteLine($"[UniversalApiProvider] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[UniversalApiProvider] Message Type: {_options.Value.MessageType}");
        Console.WriteLine($"[UniversalApiProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
