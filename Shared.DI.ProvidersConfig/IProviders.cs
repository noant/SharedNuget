namespace Shared.DI.ProvidersConfig;

public interface IProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    IReadOnlyList<TRealProvider> Providers { get; }
    TRealProvider Of(TEnumProviderType providerType);
    TRealProvider Provider { get; }
}
