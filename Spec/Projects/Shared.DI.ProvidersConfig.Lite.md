# Shared.DI.ProvidersConfig.Lite

IMPORTANT: always use AGENTS.md for rules

## Purpose
Simplified library for string-based provider configuration with DI integration.

## Dependencies
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.Configuration.Binder
- Microsoft.Extensions.Options
- Microsoft.Extensions.Options.ConfigurationExtensions

## Components

### Interfaces
- `IHasProviders<TRealProviderInterface>` - marker for holder classes
- `IProvider<TRealProviderInterface, TOptions>` - provider with options support
- `IProviders<THasProviders, TRealProviderInterface>` - provider selector with string keys
- `IProviderSwitcher<THasProviders, TRealProviderInterface>` - internal interface for default provider switching

### Implementation
- `ProviderHolder<TRealProviderInterface>` - internal storage for provider metadata
- `ProviderInfo<TRealProviderInterface>` - public provider information record
- `LiteProviders<THasProviders, TRealProviderInterface>` - provider selector implementation
- `LiteProviderSwitcher<THasProviders, TRealProviderInterface>` - default provider switcher
- `ProviderConfigurationRaw` - internal configuration model
- `ProvidersConfigurationRaw` - internal configuration collection model

### Extensions
- `ServiceCollectionExtensions.AddProvidersConfig<THasProvidersInterface, THasProviders, TRealProviderInterface>()` - DI registration

## Configuration Schema
```json
{
  "Providers": {
    "provider-key": {
      "Type": "ProviderClassName",
      "Description": "Optional description",
      "Configuration": {
        "OptionProperty1": "value1",
        "OptionProperty2": "value2"
      }
    }
  }
}
```

**Fields:**
- `provider-key` (string) - unique string identifier for the provider
- `Type` (required) - provider class name (simple name or full name)
- `Description` (optional) - description of the provider
- `Configuration` (optional) - provider-specific options bound to options class

## Key Features
- String-keyed provider selection (no enum required)
- Automatic provider discovery via reflection
- Providers registered in DI container with specified lifetime
- Options pattern integration for each provider
- Dynamic configuration reload for provider options via `IOptionsSnapshot<T>`
- Simple flat configuration structure
- Lightweight with minimal dependencies
- Provider descriptions for documentation

## Capabilities
✅ String-keyed provider selection
✅ Automatic provider discovery via reflection
✅ DI container registration for all providers
✅ Options pattern integration
✅ Dynamic options reload via `IOptionsSnapshot<T>`
✅ Simple flat configuration structure
✅ Lightweight dependencies
✅ Provider descriptions

## Limitations
❌ No enum-based provider selection
❌ No runtime provider key switching (mappings fixed at startup)
❌ No assembly type caching or configurable cache lifetime
❌ Cannot add/remove providers without restart
❌ No multiple instances of same provider class with different keys
❌ No default provider configuration exposed to users
❌ No enum-keyed hierarchical configuration

## Usage Examples

### Example 1: Basic Provider Configuration
```csharp
// Register providers
services.AddProvidersConfig<IMessageSender, MessageSender, IMessageProvider>(
    configuration,
    ServiceLifetime.Scoped,
    "Providers");

// Use providers
var providers = serviceProvider.GetRequiredService<IProviders<MessageSender, IMessageProvider>>();
var emailProvider = providers.Of("email");
await emailProvider.SendAsync("Hello!");

// Access provider information
var providerInfo = providers.Providers["email"];
Console.WriteLine($"Using: {providerInfo.Description}");
```

### Example 2: Dynamic Options Reload
```csharp
// Configuration changes in appsettings.json are automatically picked up
// via IOptionsSnapshot<T> in provider constructors

public class EmailProvider : IMessageProvider, IProvider<IMessageProvider, EmailProviderOptions>
{
    private readonly IOptionsSnapshot<EmailProviderOptions> _options;

    public EmailProvider(IOptionsSnapshot<EmailProviderOptions> options)
    {
        _options = options; // Automatically reloads when configuration changes
    }

    public Task SendAsync(string message)
    {
        // _options.Value always contains current configuration
        Console.WriteLine($"SMTP: {_options.Value.SmtpHost}:{_options.Value.SmtpPort}");
        return Task.CompletedTask;
    }
}
```

## Comparison with Full Version

| Feature | Lite | Full |
|---------|------|------|
| Provider keys | String | Enum |
| Provider registration | DI container | Manual construction |
| Configuration structure | Flat | Enum-keyed hierarchy |
| Dynamic options reload | Yes (IOptionsSnapshot) | Yes (IOptionsMonitor) |
| Runtime key switching | No | Yes (IProviderSwitcher) |
| Assembly type caching | No | Yes (configurable) |
| Multiple instances of same provider | No | Yes |
| Default provider | Internal only | Yes (configurable) |
| Dependencies | Lighter | More |
| Use case | Simple string-based selection | Complex enum-based selection with runtime switching |

## When to Use Lite vs Full

### Use Lite when:
- Simple string-based provider selection is sufficient
- Runtime provider switching is not required
- Lighter dependencies are preferred
- Multiple instances of same provider class are not needed
- Configuration reload for options is sufficient

### Use Full when:
- Enum-based type-safe provider selection is required
- Runtime provider switching via IProviderSwitcher is needed
- Multiple instances of same provider class with different configurations are required
- Assembly type caching for performance is important
- Default provider configuration is needed

## Important Notes
- All provider implementations are registered in DI container at startup
- Providers are resolved from DI container on each `Of(stringKey)` call
- Provider type mappings (string key -> provider type) are fixed at startup
- Configuration changes affect provider options only, not provider type mappings
- Use `IOptionsSnapshot<T>` in providers for dynamic configuration reload support
- The first configured provider is used as internal default
- Provider keys must be unique within configuration section
