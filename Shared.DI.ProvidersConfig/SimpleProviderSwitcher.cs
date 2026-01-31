using Microsoft.Extensions.Options;

namespace Shared.DI.ProvidersConfig;

internal class SimpleProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider> 
    : IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>
    where THasProviders : IHasProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    private TEnumProviderType _current;
    private readonly object _lock = new();

    public SimpleProviderSwitcher(IOptionsMonitor<SimpleProvidersOptions<TEnumProviderType, TRealProvider>> optionsMonitor)
    {
        var options = optionsMonitor.CurrentValue;
        
        if (options.ActiveProviders.Count == 0)
            throw new InvalidOperationException($"No active providers configured for {typeof(TRealProvider).Name}");

        if (!string.IsNullOrEmpty(options.DefaultProvider))
        {
            if (!options.ActiveProviders.ContainsKey(options.DefaultProvider))
                throw new InvalidOperationException(
                    $"Default provider '{options.DefaultProvider}' not found in active providers for {typeof(TRealProvider).Name}");

            _current = (TEnumProviderType)Enum.Parse(typeof(TEnumProviderType), options.DefaultProvider);
        }
        else
        {
            var firstActiveKey = options.ActiveProviders.Keys.First();
            _current = (TEnumProviderType)Enum.Parse(typeof(TEnumProviderType), firstActiveKey);
        }
    }

    public TEnumProviderType Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
        set
        {
            lock (_lock)
            {
                _current = value;
            }
        }
    }
}
