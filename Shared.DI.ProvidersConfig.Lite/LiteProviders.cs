using Microsoft.Extensions.DependencyInjection;
using Shared.DI.ProvidersConfig.Lite.Abstractions;

namespace Shared.DI.ProvidersConfig.Lite
{
    internal class LiteProviders<THasProviders, TRealProviderInterface> : IProviders<THasProviders, TRealProviderInterface>
        where TRealProviderInterface : class
        where THasProviders : class, IHasProviders<TRealProviderInterface>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProviderSwitcher<THasProviders, TRealProviderInterface> _switcher;

        public LiteProviders(
            IEnumerable<ProviderHolder<TRealProviderInterface>> holders,
            IServiceProvider serviceProvider,
            IProviderSwitcher<THasProviders, TRealProviderInterface> switcher)
        {
            _serviceProvider = serviceProvider;
            _switcher = switcher;
            Providers = holders
                .ToDictionary(
                    x => x.Key,
                    x => new ProviderInfo<TRealProviderInterface>(
                        x.Key,
                        _serviceProvider.GetRequiredKeyedService(x.ProviderType, x.Key) as TRealProviderInterface,
                        x.Description));
        }

        public TRealProviderInterface Provider => 
            string.IsNullOrEmpty(_switcher.CurrentKey) 
            ? Providers.Values.First().Provider 
            : Of(_switcher.CurrentKey);

        public IReadOnlyDictionary<string, ProviderInfo<TRealProviderInterface>> Providers { get; private set; }

        public TRealProviderInterface Of(string providerKey) => Providers[providerKey].Provider;
    }
}