namespace Shared.DI.ProvidersConfig;

internal class SimpleProvidersOptions<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    public string? DefaultProvider { get; set; }
    public Dictionary<string, string> ActiveProviders { get; set; } = new();
    public Dictionary<string, object> Configurations { get; set; } = new();
    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromSeconds(15);
    public bool ReloadAssemblyInfo { get; set; } = false;
}
