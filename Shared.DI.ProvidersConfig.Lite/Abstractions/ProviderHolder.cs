using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    internal record ProviderHolder<TRealProviderInterface>(string Key, Type ProviderType, Type OptionsType, string Description)
        where TRealProviderInterface : class;

    public record ProviderInfo<TRealProviderInterface>(string Key, TRealProviderInterface Provider, string Description)
        where TRealProviderInterface : class;
}