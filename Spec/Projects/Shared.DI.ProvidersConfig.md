# Shared.DI.ProvidersConfig

IMPORTANT: always use AGENTS.md for rules

## Purpose
Library for configuration-driven provider selection with DI integration.

## Dependencies
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.Configuration.Binder
- Microsoft.Extensions.Options
- Microsoft.Extensions.Options.ConfigurationExtensions

## Components

### Interfaces
- `IHasProviders<TEnumProviderType, TRealProvider>` - marker for holder classes
- `IProvider<TRealProvider, TOptions>` - provider with options support
- `IProviders<TEnumProviderType, TRealProvider>` - provider selector
- `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>` - runtime provider switching

### Implementation
- `ProviderHolder<TEnumProviderType, TProvider>` - internal storage
- `SimpleProviders<TEnumProviderType, TRealProvider>` - provider selector implementation
- `SimpleProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>` - thread-safe provider switching
- `SimpleProvidersOptions<TEnumProviderType, TRealProvider>` - configuration options with defaultProvider support

### Extensions
- `ServiceCollectionExtensions.AddProvidersConfiguration<THasProviders>()` - DI registration with generic interface support

## Configuration Schema
```json
{
  "providersConfiguration": {
    "{THasProviders.Name}": {
      "cacheLifetime": "00:00:15",
      "reloadAssemblyInfo": false,
      "defaultProvider": "EnumValue",
      "activeProviders": {
        "EnumValue": "ProviderClassName"
      },
      "configurations": {
        "EnumValue": {
          "ProviderClassName": { }
        }
      }
    }
  }
}
```

**Fields:**
- `cacheLifetime` (optional, default: `00:00:15`) - TimeSpan for assembly type cache expiration
- `reloadAssemblyInfo` (optional, default: `false`) - if `false`, assembly type cache never expires after first load
- `defaultProvider` (optional) - enum key for default provider used by `IProviders.Provider`
- `activeProviders` (required) - dictionary mapping enum keys to provider class names
- `configurations` (optional) - enum-keyed nested dictionary: `{EnumKey}.{ProviderClassName}.{ProviderOptions}`

## Key Features
- Automatic provider discovery via reflection (no DI registration)
- Manual provider construction without DI container pollution
- Enum-keyed hierarchical configuration structure
- Assembly type caching with configurable lifetime
- Support for multiple instances of same provider class with different configurations
- Automatic IOptions configuration for each provider from JSON
- Support for runtime provider selection via IProviders<>
- Default provider configuration with runtime switching via IProviderSwitcher
- Thread-safe provider switching with persistent state across DI lifetimes
- Dynamic configuration reload support
- Recommended Singleton lifetime for optimal caching performance
- Generic interface resolution support for `IHasProviders<,>` and `IProviderSwitcher<IHasProviders<,>,,>`

## Usage Examples

### Example 1: Multiple Providers
```csharp
// Recommended: Singleton lifetime for optimal caching
// Automatically registers MessageSender in DI
services.AddProvidersConfiguration<MessageSender>(configuration);

// Runtime selection
var providers = serviceProvider.GetRequiredService<IProviders<MessageType, IMessageProvider>>();
var emailProvider = providers.Of(MessageType.Email);

// Default provider (from configuration or IProviderSwitcher)
var defaultProvider = providers.Provider;

// Runtime switching
var switcher = serviceProvider.GetRequiredService<IProviderSwitcher<MessageSender, MessageType, IMessageProvider>>();
switcher.Current = MessageType.Sms; // Changes default provider

// Generic interface resolution
var messageSender = serviceProvider.GetRequiredService<IHasProviders<MessageType, IMessageProvider>>();
var genericSwitcher = serviceProvider.GetRequiredService<IProviderSwitcher<IHasProviders<MessageType, IMessageProvider>, MessageType, IMessageProvider>>();
```

### Example 2: Multiple Instances of Same Provider
```csharp
// Configuration:
// "activeProviders": {
//   "Chat": "OpenAiLlmProvider",
//   "Reasoner": "OpenAiLlmProvider"
// }
// "configurations": {
//   "Chat": { "OpenAiLlmProvider": { "ModelName": "deepseek-chat" } },
//   "Reasoner": { "OpenAiLlmProvider": { "ModelName": "deepseek-reasoner" } }
// }

var providers = serviceProvider.GetRequiredService<IProviders<LlmType, ILlmProvider>>();
var chatProvider = providers.Of(LlmType.Chat);        // OpenAiLlmProvider with "deepseek-chat"
var reasonerProvider = providers.Of(LlmType.Reasoner); // OpenAiLlmProvider with "deepseek-reasoner"
```

## Important Notes
- Provider implementations are NOT registered in DI - they are constructed manually
- Providers are constructed fresh on each `Of(enumKey)` call
- IOptions<TOptions> is created manually from configuration during provider construction
- Holder classes are automatically registered in DI by `AddProvidersConfiguration`
- Configuration reload requires `reloadOnChange: true` in ConfigurationBuilder
- Assembly type scanning is cached based on `cacheLifetime` and `reloadAssemblyInfo` settings
- Singleton lifetime (default) is recommended for `AddProvidersConfiguration` for optimal caching performance
- THasProviders classes are registered as both concrete type and `IHasProviders<TEnumProviderType, TRealProvider>`
- IProviderSwitcher is registered as both `IProviderSwitcher<THasProviders, ...>` and `IProviderSwitcher<IHasProviders<...>, ...>`
