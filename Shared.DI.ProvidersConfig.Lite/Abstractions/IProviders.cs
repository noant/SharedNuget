namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    public interface IProviders<THasProviders, TRealProviderInterface>
        where THasProviders : class, IHasProviders<TRealProviderInterface>
        where TRealProviderInterface : class
    {
        TRealProviderInterface Provider { get; }
        IReadOnlyDictionary<string, ProviderInfo<TRealProviderInterface>> Providers { get; }

        TRealProviderInterface Of(string providerKey);
    }
}