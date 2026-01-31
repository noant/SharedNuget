namespace Shared.DI.ProvidersConfig;

public interface IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>
    where THasProviders : IHasProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    TEnumProviderType Current { get; set; }
}
