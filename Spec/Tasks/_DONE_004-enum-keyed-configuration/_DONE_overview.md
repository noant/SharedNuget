# Task 004: Enum-Keyed Configuration and Manual Provider Construction

## Type
Feature

## Summary
Refactor provider configuration to support enum-keyed hierarchical structure, manual provider construction without DI registration, and assembly type caching with configurable lifetime.

## IMPORTANT
Always use AGENTS.md for rules.

## As-Is
- All provider options are registered in DI via `ServiceCollectionExtensions`.
- Configuration structure: `providersConfiguration.{IHasProviders}.configurations.{ProviderClassName}`.
- Providers are resolved from DI as `IEnumerable<TRealProvider>`.
- All provider types are registered in DI container.
- No caching of assembly type scanning results.
- No support for multiple instances of same provider class with different configurations.

## To-Be
- Provider options are NOT registered in DI.
- Configuration structure: `providersConfiguration.{IHasProviders}.configurations.{EnumKey}.{ProviderClassName}`.
- Providers are constructed manually by resolving constructor parameters from DI and configuration.
- Provider types are discovered via assembly scanning, not DI registration.
- Assembly type scanning results are cached with configurable lifetime.
- Support multiple instances of same provider class with different enum keys and configurations.
- `AddProvidersConfiguration<T>` registered as Singleton by default (recommended lifetime).

## Configuration Structure

### New Configuration Format

```json
{
  "providersConfiguration": {
    "MessageSender": {
      "cacheLifetime": "00:00:15",
      "reloadAssemblyInfo": false,
      "defaultProvider": "Email",
      "activeProviders": {
        "Email": "EmailProvider",
        "Sms": "SmsProvider"
      },
      "configurations": {
        "Email": {
          "EmailProvider": {
            "SmtpHost": "smtp.example.com",
            "SmtpPort": 587
          }
        },
        "Sms": {
          "SmsProvider": {
            "ApiKey": "your-api-key-here",
            "ApiUrl": "https://api.sms-provider.com"
          },
          "SecondarySmsProvider": {
            "ApiKey": "secondary-api-key",
            "ApiUrl": "https://api.secondary-sms.com",
            "ProviderName": "Secondary SMS Service"
          }
        }
      }
    }
  }
}
```

### Multiple Instances Example

```json
{
  "providersConfiguration": {
    "LlmService": {
      "cacheLifetime": "00:00:15",
      "reloadAssemblyInfo": false,
      "defaultProvider": "Chat",
      "activeProviders": {
        "Chat": "OpenAiLlmProvider",
        "Reasoner": "OpenAiLlmProvider"
      },
      "configurations": {
        "Chat": {
          "OpenAiLlmProvider": {
            "ApiKey": "apikey",
            "ModelName": "deepseek-chat",
            "Uri": "https://api.deepseek.com/v1"
          }
        },
        "Reasoner": {
          "OpenAiLlmProvider": {
            "ApiKey": "apikey",
            "ModelName": "deepseek-reasoner",
            "Uri": "https://api.deepseek.com/v1"
          },
          "AnotherReasonerLlmProvider": {
            "Token": "token",
            "Address": "https://api.somellm.com"
          }
        }
      }
    }
  }
}
```

### Configuration Fields

- `cacheLifetime` (optional, default: `00:00:15`): TimeSpan for assembly type cache expiration.
- `reloadAssemblyInfo` (optional, default: `false`): If `false`, cache never expires after first load.
- `defaultProvider` (required): Default enum key for provider selection.
- `activeProviders` (required): Dictionary mapping enum keys to provider class names.
- `configurations` (required): Nested dictionary: `{EnumKey}.{ProviderClassName}.{ProviderOptions}`.

## Components

### 1. Remove DI Registration of Providers
**Project:** `Shared.DI.ProvidersConfig`
**File:** `ServiceCollectionExtensions.cs`

Changes:
- Remove registration of all provider types in DI container.
- Remove registration of `IEnumerable<TRealProvider>`.
- Remove options binding for provider options (`IOptions<TOptions>`, `IOptionsSnapshot<TOptions>`).
- Keep registration of `IProviders<TEnumProviderType, TRealProvider>` and `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>`.

### 2. Assembly Type Discovery and Caching
**Project:** `Shared.DI.ProvidersConfig`
**File:** `SimpleProviders.cs`

New methods:
- `private IEnumerable<Type> GetRelevantProviderTypes()`: Scans all loaded assemblies for types implementing `IProvider<TRealProvider, TOptions>` where `TRealProvider` matches current provider type.
- `private IDictionary<TEnumType, Type> GetRelevantProviderDict()`: Combines `GetRelevantProviderTypes()` with `activeProviders` configuration to create enum-to-type mapping.

Caching requirements for `GetRelevantProviderTypes()`:
- Cache results with expiration based on `cacheLifetime` configuration.
- If `reloadAssemblyInfo == false`, cache NEVER expires (cached forever after first load).
- If `reloadAssemblyInfo == true`, cache expires after `cacheLifetime` and is reloaded.
- Thread-safe cache implementation (use `lock` or `SemaphoreSlim`).
- All caching logic is internal to the method.

Caching requirements for `GetRelevantProviderDict()`:
- Cache results with expiration based on `cacheLifetime` configuration.
- Cache ALWAYS expires after `cacheLifetime` regardless of `reloadAssemblyInfo` setting.
- Thread-safe cache implementation (use `lock` or `SemaphoreSlim`).
- All caching logic is internal to the method.

### 3. Manual Provider Construction
**Project:** `Shared.DI.ProvidersConfig`
**File:** `SimpleProviders.cs`

New classes:
- `SimpleOptionsHolder<TOptions>`: Wrapper for `IOptions<TOptions>` interface.
- `SimpleOptionsSnapshot<TOptions>`: Wrapper for `IOptionsSnapshot<TOptions>` interface.

Constructor parameter resolution:
- All constructor parameters except options are resolved from `IServiceProvider`.
- Options parameters (`IOptions<TOptions>`, `IOptionsSnapshot<TOptions>`) are resolved as follows:
  1. Get configuration section: `configurations.{EnumKey}.{ProviderClassName}`.
  2. Bind configuration to `TOptions` instance.
  3. Create `SimpleOptionsHolder<TOptions>` or `SimpleOptionsSnapshot<TOptions>` with bound options.
  4. Pass wrapper to provider constructor.

### 4. Update Of() Method
**Project:** `Shared.DI.ProvidersConfig`
**File:** `SimpleProviders.cs`

Changes:
- Use `GetRelevantProviderDict()` to get enum-to-type mapping.
- Construct provider instance manually using reflection and parameter resolution.
- Cache constructed provider instances per enum key (optional optimization).

### 5. Update Configuration Options
**Project:** `Shared.DI.ProvidersConfig`
**File:** `SimpleProvidersOptions.cs`

Add fields:
- `TimeSpan CacheLifetime { get; set; }` (default: `TimeSpan.FromSeconds(15)`)
- `bool ReloadAssemblyInfo { get; set; }` (default: `false`)

### 6. Update ServiceCollectionExtensions
**Project:** `Shared.DI.ProvidersConfig`
**File:** `ServiceCollectionExtensions.cs`

Changes:
- Change default `lifetime` parameter to `ServiceLifetime.Singleton` in `AddProvidersConfiguration<T>` methods.
- Pass `IConfiguration` instance to `SimpleProviders<THasProviders, TEnumProviderType, TRealProvider>` constructor.
- Remove all provider type registration logic.

### 7. Update README
**Project:** `Shared.DI.ProvidersConfig`
**File:** `README.md`

Add recommendations:
- Recommend registering `AddProvidersConfiguration<T>` as Singleton for optimal caching performance.
- Document new configuration structure with enum-keyed hierarchy.
- Document `cacheLifetime` and `reloadAssemblyInfo` configuration options.
- Provide examples of multiple instances of same provider class.

## Implementation Details

### SimpleOptionsHolder and SimpleOptionsSnapshot

```csharp
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
```

### Assembly Type Caching

```csharp
private class TypeCache
{
    public IEnumerable<Type> Types { get; set; }
    public DateTime ExpiresAt { get; set; }
}

private class ProviderDictCache
{
    public IDictionary<TEnumProviderType, Type> Dict { get; set; }
    public DateTime ExpiresAt { get; set; }
}

private TypeCache? _typeCache;
private readonly object _typeCacheLock = new();

private ProviderDictCache? _providerDictCache;
private readonly object _providerDictCacheLock = new();

private IEnumerable<Type> GetRelevantProviderTypes()
{
    var options = GetOptions();
    
    lock (_typeCacheLock)
    {
        // If reloadAssemblyInfo == false, cache never expires
        if (_typeCache != null && !options.ReloadAssemblyInfo)
        {
            return _typeCache.Types;
        }

        // If reloadAssemblyInfo == true, check expiration
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
        // Cache always expires after cacheLifetime regardless of reloadAssemblyInfo
        if (_providerDictCache != null && DateTime.UtcNow < _providerDictCache.ExpiresAt)
        {
            return _providerDictCache.Dict;
        }

        var providerTypes = GetRelevantProviderTypes();
        var activeProviders = ProviderTypes;
        var dict = new Dictionary<TEnumProviderType, Type>();

        foreach (var kvp in activeProviders)
        {
            var enumKey = Enum.Parse<TEnumProviderType>(kvp.Key);
            var providerName = kvp.Value;
            
            var providerType = providerTypes.FirstOrDefault(t => 
                t.Name == providerName || t.FullName == providerName);
            
            if (providerType != null)
            {
                dict[enumKey] = providerType;
            }
        }

        _providerDictCache = new ProviderDictCache
        {
            Dict = dict,
            ExpiresAt = DateTime.UtcNow.Add(options.CacheLifetime)
        };

        return dict;
    }
}
```

### Reflection Metadata Caching

For optimal performance, cache reflection metadata to avoid repeated expensive operations:

```csharp
private class ConstructorMetadata
{
    public ConstructorInfo Constructor { get; set; }
    public ParameterInfo[] Parameters { get; set; }
}

private class WrapperConstructorMetadata
{
    public ConstructorInfo Constructor { get; set; }
    public Type GenericType { get; set; }
}

private readonly ConcurrentDictionary<Type, ConstructorMetadata> _constructorCache = new();
private readonly ConcurrentDictionary<Type, WrapperConstructorMetadata> _optionsHolderCache = new();
private readonly ConcurrentDictionary<Type, WrapperConstructorMetadata> _optionsSnapshotCache = new();

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
        var constructor = genericType.GetConstructor(new[] { t });
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
        var constructor = genericType.GetConstructor(new[] { t });
        return new WrapperConstructorMetadata
        {
            GenericType = genericType,
            Constructor = constructor
        };
    });
}
```

**Performance rationale:**
- `GetConstructors()` and `GetParameters()` are expensive reflection operations (~10x slower than direct calls)
- `MakeGenericType()` has lookup overhead even though CLR caches the resulting types
- For parameterless constructors: `Activator.CreateInstance()` is ~1.7x faster than `ConstructorInfo.Invoke()`
- For constructors with parameters: `ConstructorInfo.Invoke()` is ~2x faster than `Activator.CreateInstance()`
- Caching eliminates repeated reflection calls when constructing providers multiple times
- Especially important when providers have Scoped or Transient lifetime

### Provider Construction

```csharp
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
                
                // Use Activator.CreateInstance for parameterless constructor (1.7x faster than ConstructorInfo.Invoke)
                var optionsInstance = Activator.CreateInstance(optionsType);
                configSection.Bind(optionsInstance);

                if (genericTypeDef == typeof(IOptions<>))
                {
                    var holderMetadata = GetOptionsHolderMetadata(optionsType);
                    // Use ConstructorInfo.Invoke for constructor with parameters (2x faster than Activator.CreateInstance)
                    args[i] = holderMetadata.Constructor.Invoke(new[] { optionsInstance });
                }
                else
                {
                    var snapshotMetadata = GetOptionsSnapshotMetadata(optionsType);
                    // Use ConstructorInfo.Invoke for constructor with parameters (2x faster than Activator.CreateInstance)
                    args[i] = snapshotMetadata.Constructor.Invoke(new[] { optionsInstance });
                }
                
                continue;
            }
        }

        args[i] = _serviceProvider.GetRequiredService(paramType);
    }

    return (TRealProvider)metadata.Constructor.Invoke(args);
}
```

## Error Handling
- Throw exception if provider type not found in assembly scan results.
- Throw exception if provider constructor cannot be resolved.
- Throw exception if configuration section not found for provider.
- Throw exception if enum key not found in `activeProviders`.
- Throw exception if options binding fails.

## Benefits
- Support multiple instances of same provider class with different configurations.
- Reduced DI container pollution (no provider type registration).
- Improved performance via assembly type caching.
- Flexible configuration structure with enum-keyed hierarchy.
- Better control over provider lifecycle and construction.
