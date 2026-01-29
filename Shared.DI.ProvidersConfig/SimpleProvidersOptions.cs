namespace Shared.DI.ProvidersConfig;

internal class SimpleProvidersOptions<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    public Dictionary<string, string> ActiveProviders { get; set; } = new();
    public Dictionary<string, object> Configurations { get; set; } = new();
}
