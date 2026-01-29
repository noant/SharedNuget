namespace Shared.DI.ProvidersConfig;

public interface IHasProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
}
