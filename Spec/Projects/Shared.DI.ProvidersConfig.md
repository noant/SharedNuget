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
- `ServiceCollectionExtensions.AddProvidersConfiguration<THasProviders>()` - DI registration

## Configuration Schema
```json
{
  "providersConfiguration": {
    "{THasProviders.Name}": {
      "defaultProvider": "EnumValue",
      "activeProviders": {
        "EnumValue": "ProviderClassName"
      },
      "configurations": {
        "ProviderClassName": { }
      }
    }
  }
}
```

**Fields:**
- `defaultProvider` (optional) - enum key for default provider used by `IProviders.Provider`
- `activeProviders` (required) - dictionary mapping enum keys to provider class names
- `configurations` (optional) - provider-specific configuration objects

## Key Features
- Automatic provider discovery and registration via reflection
- No manual DI registration needed for provider implementations
- Automatic IOptions configuration for each provider from JSON
- Support for runtime provider selection via IProviders<>
- Support for direct single provider injection
- Default provider configuration with runtime switching via IProviderSwitcher
- Thread-safe provider switching with persistent state across DI lifetimes
- Dynamic configuration reload support

## Usage Examples

### Example 1: Multiple Providers
```csharp
services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<MessageSender>();

// Runtime selection
var providers = serviceProvider.GetRequiredService<IProviders<MessageType, IMessageProvider>>();
var emailProvider = providers.Of(MessageType.Email);

// Default provider (from configuration or IProviderSwitcher)
var defaultProvider = providers.Provider;

// Runtime switching
var switcher = serviceProvider.GetRequiredService<IProviderSwitcher<MessageSender, MessageType, IMessageProvider>>();
switcher.Current = MessageType.Sms; // Changes default provider
```

### Example 2: Single Provider Direct Injection
```csharp
services.AddProvidersConfiguration<NotificationService>(configuration, ServiceLifetime.Scoped);
services.AddScoped<NotificationService>();

// Direct provider injection - automatically registered by library
var telegramProvider = serviceProvider.GetRequiredService<TelegramProvider>();
await telegramProvider.SendAsync("Message");

// Or via interface
var notificationProvider = serviceProvider.GetRequiredService<INotificationProvider>();
await notificationProvider.SendAsync("Message");
```

## Important Notes
- Provider implementations are automatically registered in DI by the library
- IOptions<TOptions> is automatically configured from configurations section
- Only holder classes need manual DI registration
- Provider classes and IOptions are registered automatically
