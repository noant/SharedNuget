using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shared.DI.ProvidersConfig;

internal class SimpleProviders<TEnumProviderType, TRealProvider> : IProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<TRealProvider> _allProviders;

    public SimpleProviders(
        IServiceProvider serviceProvider,
        IEnumerable<TRealProvider> allProviders)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _allProviders = allProviders ?? throw new ArgumentNullException(nameof(allProviders));
    }

    internal IReadOnlyDictionary<string, string> ProviderTypes
    {
        get
        {
            var optionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<SimpleProvidersOptions<TEnumProviderType, TRealProvider>>>();
            return optionsMonitor.CurrentValue.ActiveProviders;
        }
    }

    private IEnumerable<TRealProvider> GetFilteredProviders()
    {
        var activeProviders = ProviderTypes;
        var activeProviderNames = activeProviders.Values.ToHashSet();
        
        return _allProviders.Where(p =>
        {
            var providerType = p.GetType();
            return activeProviderNames.Contains(providerType.Name) || 
                   activeProviderNames.Contains(providerType.FullName!);
        });
    }

    public IReadOnlyList<TRealProvider> Providers
    {
        get
        {
            return GetFilteredProviders()
                .ToList()
                .AsReadOnly();
        }
    }

    public TRealProvider Provider
    {
        get
        {
            var provider = GetFilteredProviders().FirstOrDefault();
            
            if (provider == null)
                throw new InvalidOperationException($"No providers configured for {typeof(TRealProvider).Name}");

            return provider;
        }
    }

    public TRealProvider Of(TEnumProviderType providerType)
    {
        var activeProviders = ProviderTypes;
        var enumKey = providerType.ToString();
        
        if (!activeProviders.TryGetValue(enumKey, out var providerName))
            throw new InvalidOperationException(
                $"Provider for type {providerType} not found in active providers for {typeof(TRealProvider).Name}");

        var provider = GetFilteredProviders()
            .FirstOrDefault(p =>
            {
                var type = p.GetType();
                return type.Name == providerName || type.FullName == providerName;
            });

        if (provider == null)
            throw new InvalidOperationException(
                $"Provider '{providerName}' for type {providerType} not found");

        return provider;
    }
}
