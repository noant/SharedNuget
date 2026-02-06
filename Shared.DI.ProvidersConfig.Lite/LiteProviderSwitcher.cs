using Shared.DI.ProvidersConfig.Lite.Abstractions;

namespace Shared.DI.ProvidersConfig.Lite
{
    internal class LiteProviderSwitcher<THasProviders, TRealProviderInterface> : IProviderSwitcher<THasProviders, TRealProviderInterface>
        where THasProviders : IHasProviders<TRealProviderInterface>
        where TRealProviderInterface : class
    {
        public LiteProviderSwitcher(ProvidersConfigurationRaw configuration)
        {
            CurrentKey = configuration.Default ?? configuration.Keys.FirstOrDefault();
        }

        public string CurrentKey { get; set; }
    }
}