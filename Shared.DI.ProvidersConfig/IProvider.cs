namespace Shared.DI.ProvidersConfig;

public interface IProvider<TRealProvider, TOptions>
    where TRealProvider : class
    where TOptions : class
{
}
