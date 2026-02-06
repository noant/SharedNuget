using Microsoft.Extensions.Options;

namespace Shared.DI.ProvidersConfig.Lite.Abstractions
{
    internal class StubOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        private readonly TOptions _value;
        private readonly string _key;

        public StubOptionsSnapshot(TOptions value, string key)
        {
            _value = value;
            _key = key;
        }

        public TOptions Value => _value;

        public TOptions Get(string? name)
        {
            if (name != _key)
            {
                throw new InvalidOperationException(
                    $"StubOptionsSnapshot was created for key '{_key}', but requested key '{name}'.");
            }
            
            return _value;
        }
    }
}
