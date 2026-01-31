using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace Shared.DI.ProvidersConfig;

internal class SimpleOptionsHolder<TOptions> : IOptions<TOptions>
    where TOptions : class
{
    public TOptions Value { get; }

    public SimpleOptionsHolder(TOptions value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}

internal class SimpleOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
    where TOptions : class
{
    public TOptions Value { get; }

    public SimpleOptionsSnapshot(TOptions value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public TOptions Get(string? name) => Value;
}

internal class SimpleProviders<THasProviders, TEnumProviderType, TRealProvider> : IProviders<TEnumProviderType, TRealProvider>
    where THasProviders : IHasProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider> _providerSwitcher;
    private readonly IConfiguration _configuration;
    private readonly string _configSectionName;

    private class TypeCache
    {
        public IEnumerable<Type> Types { get; set; } = Enumerable.Empty<Type>();
        public DateTime ExpiresAt { get; set; }
    }

    private class ProviderDictCache
    {
        public IDictionary<TEnumProviderType, Type> Dict { get; set; } = new Dictionary<TEnumProviderType, Type>();
        public DateTime ExpiresAt { get; set; }
    }

    private class ConstructorMetadata
    {
        public ConstructorInfo Constructor { get; set; } = null!;
        public ParameterInfo[] Parameters { get; set; } = Array.Empty<ParameterInfo>();
    }

    private class WrapperConstructorMetadata
    {
        public ConstructorInfo Constructor { get; set; } = null!;
        public Type GenericType { get; set; } = null!;
    }

    private TypeCache? _typeCache;
    private readonly object _typeCacheLock = new();

    private ProviderDictCache? _providerDictCache;
    private readonly object _providerDictCacheLock = new();

    private readonly ConcurrentDictionary<Type, ConstructorMetadata> _constructorCache = new();
    private readonly ConcurrentDictionary<Type, WrapperConstructorMetadata> _optionsHolderCache = new();
    private readonly ConcurrentDictionary<Type, WrapperConstructorMetadata> _optionsSnapshotCache = new();

    public SimpleProviders(
        IServiceProvider serviceProvider,
        IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider> providerSwitcher,
        IConfiguration configuration,
        string configSectionName)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _providerSwitcher = providerSwitcher ?? throw new ArgumentNullException(nameof(providerSwitcher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configSectionName = configSectionName ?? throw new ArgumentNullException(nameof(configSectionName));
    }

    internal IReadOnlyDictionary<string, string> ProviderTypes => 
        _serviceProvider
            .GetRequiredService<IOptionsMonitor<SimpleProvidersOptions<TEnumProviderType, TRealProvider>>>()
            .CurrentValue
            .ActiveProviders;

    private SimpleProvidersOptions<TEnumProviderType, TRealProvider> GetOptions() =>
        _serviceProvider
            .GetRequiredService<IOptionsMonitor<SimpleProvidersOptions<TEnumProviderType, TRealProvider>>>()
            .CurrentValue;

    private IEnumerable<Type> GetRelevantProviderTypes()
    {
        var options = GetOptions();
        
        lock (_typeCacheLock)
        {
            if (_typeCache != null && !options.ReloadAssemblyInfo)
            {
                return _typeCache.Types;
            }

            if (_typeCache != null && options.ReloadAssemblyInfo && DateTime.UtcNow < _typeCache.ExpiresAt)
            {
                return _typeCache.Types;
            }

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => 
                    t.IsClass && 
                    !t.IsAbstract && 
                    typeof(TRealProvider).IsAssignableFrom(t))
                .ToList();

            _typeCache = new TypeCache
            {
                Types = types,
                ExpiresAt = DateTime.UtcNow.Add(options.CacheLifetime)
            };

            return types;
        }
    }

    private IDictionary<TEnumProviderType, Type> GetRelevantProviderDict()
    {
        var options = GetOptions();
        
        lock (_providerDictCacheLock)
        {
            if (_providerDictCache != null && DateTime.UtcNow < _providerDictCache.ExpiresAt)
            {
                return _providerDictCache.Dict;
            }

            var providerTypes = GetRelevantProviderTypes();
            var activeProviders = ProviderTypes;
            var dict = new Dictionary<TEnumProviderType, Type>();

            foreach (var kvp in activeProviders)
            {
                var enumKey = (TEnumProviderType)Enum.Parse(typeof(TEnumProviderType), kvp.Key);
                var providerName = kvp.Value;
                
                var providerType = providerTypes.FirstOrDefault(t => 
                    t.Name == providerName || t.FullName == providerName);
                
                if (providerType == null)
                    throw new InvalidOperationException(
                        $"Provider type '{providerName}' not found in loaded assemblies for enum key '{kvp.Key}'");

                dict[enumKey] = providerType;
            }

            _providerDictCache = new ProviderDictCache
            {
                Dict = dict,
                ExpiresAt = DateTime.UtcNow.Add(options.CacheLifetime)
            };

            return dict;
        }
    }

    private ConstructorMetadata GetConstructorMetadata(Type providerType)
    {
        return _constructorCache.GetOrAdd(providerType, t =>
        {
            var constructors = t.GetConstructors();
            if (constructors.Length == 0)
                throw new InvalidOperationException($"No public constructors found for {t.Name}");

            var constructor = constructors[0];
            return new ConstructorMetadata
            {
                Constructor = constructor,
                Parameters = constructor.GetParameters()
            };
        });
    }

    private WrapperConstructorMetadata GetOptionsHolderMetadata(Type optionsType)
    {
        return _optionsHolderCache.GetOrAdd(optionsType, t =>
        {
            var genericType = typeof(SimpleOptionsHolder<>).MakeGenericType(t);
            var constructor = genericType.GetConstructor(new[] { t })
                ?? throw new InvalidOperationException($"Constructor not found for SimpleOptionsHolder<{t.Name}>");
            return new WrapperConstructorMetadata
            {
                GenericType = genericType,
                Constructor = constructor
            };
        });
    }

    private WrapperConstructorMetadata GetOptionsSnapshotMetadata(Type optionsType)
    {
        return _optionsSnapshotCache.GetOrAdd(optionsType, t =>
        {
            var genericType = typeof(SimpleOptionsSnapshot<>).MakeGenericType(t);
            var constructor = genericType.GetConstructor(new[] { t })
                ?? throw new InvalidOperationException($"Constructor not found for SimpleOptionsSnapshot<{t.Name}>");
            return new WrapperConstructorMetadata
            {
                GenericType = genericType,
                Constructor = constructor
            };
        });
    }

    private IConfigurationSection GetProviderConfigSection(TEnumProviderType enumKey, Type providerType)
    {
        var configSection = _configuration.GetSection(_configSectionName);
        var configurationsSection = configSection.GetSection("configurations");
        var enumKeySection = configurationsSection.GetSection(enumKey.ToString()!);
        var providerConfigSection = enumKeySection.GetSection(providerType.Name);

        if (!providerConfigSection.Exists())
            throw new InvalidOperationException(
                $"Configuration section '{_configSectionName}:configurations:{enumKey}:{providerType.Name}' not found");

        return providerConfigSection;
    }

    private TRealProvider ConstructProvider(Type providerType, TEnumProviderType enumKey)
    {
        var metadata = GetConstructorMetadata(providerType);
        var args = new object?[metadata.Parameters.Length];

        for (int i = 0; i < metadata.Parameters.Length; i++)
        {
            var param = metadata.Parameters[i];
            var paramType = param.ParameterType;

            if (paramType.IsGenericType)
            {
                var genericTypeDef = paramType.GetGenericTypeDefinition();
                
                if (genericTypeDef == typeof(IOptions<>) || genericTypeDef == typeof(IOptionsSnapshot<>))
                {
                    var optionsType = paramType.GetGenericArguments()[0];
                    var configSection = GetProviderConfigSection(enumKey, providerType);
                    
                    var optionsInstance = Activator.CreateInstance(optionsType)
                        ?? throw new InvalidOperationException($"Failed to create instance of {optionsType.Name}");
                    configSection.Bind(optionsInstance);

                    if (genericTypeDef == typeof(IOptions<>))
                    {
                        var holderMetadata = GetOptionsHolderMetadata(optionsType);
                        args[i] = holderMetadata.Constructor.Invoke(new[] { optionsInstance });
                    }
                    else
                    {
                        var snapshotMetadata = GetOptionsSnapshotMetadata(optionsType);
                        args[i] = snapshotMetadata.Constructor.Invoke(new[] { optionsInstance });
                    }
                    
                    continue;
                }
            }

            args[i] = _serviceProvider.GetRequiredService(paramType);
        }

        return (TRealProvider)metadata.Constructor.Invoke(args);
    }

    public IReadOnlyList<TRealProvider> Providers
    {
        get
        {
            var providerDict = GetRelevantProviderDict();
            return providerDict.Keys
                .Select(enumKey => ConstructProvider(providerDict[enumKey], enumKey))
                .ToList()
                .AsReadOnly();
        }
    }

    public TRealProvider Provider => Of(_providerSwitcher.Current);

    public TRealProvider Of(TEnumProviderType providerType)
    {
        var providerDict = GetRelevantProviderDict();
        
        if (!providerDict.TryGetValue(providerType, out var providerTypeToConstruct))
            throw new InvalidOperationException(
                $"Provider for type {providerType} not found in active providers for {typeof(TRealProvider).Name}");

        return ConstructProvider(providerTypeToConstruct, providerType);
    }
}
