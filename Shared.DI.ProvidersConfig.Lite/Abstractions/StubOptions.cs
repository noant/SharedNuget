using Microsoft.Extensions.Options;

namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    internal class StubOptions<TOptions> : IOptions<TOptions>
        where TOptions : class
    {
        private readonly TOptions _value;

        public StubOptions(TOptions value)
        {
            _value = value;
        }

        public TOptions Value => _value;
    }
}
