using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    public interface IProviderSwitcher<THasProviders, TRealProviderInterface>
        where THasProviders : IHasProviders<TRealProviderInterface>
        where TRealProviderInterface : class
    {
        string CurrentKey { get; set; }
    }
}
