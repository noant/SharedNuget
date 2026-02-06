namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    internal class ProviderConfigurationRaw
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }

    internal class ProvidersConfigurationRaw : Dictionary<string, ProviderConfigurationRaw>
    {
        public string Default { get; set; }
    }
}