namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    public interface IProvider<TRealProviderInterface, TRealProviderOptions>
        where TRealProviderInterface : class
        where TRealProviderOptions : class
    {
    }
}