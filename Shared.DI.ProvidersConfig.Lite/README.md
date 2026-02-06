# Shared.DI.ProvidersConfig.Lite

Provider configuration system with string-based keys and DI integration.

## Features

- **String-Based Provider Keys**: Simple string keys for provider selection
  ```csharp
  // Select specific provider by key
  var smsProvider = _providers.Of("sms");
  await smsProvider.SendAsync("Hello via SMS!");
  ```
- **Runtime Provider Switching**: Change default provider at runtime via `IProviderSwitcher`
- **Default Provider Support**: Use `_providers.Provider` to access the current default provider
- **DI Container Registration**: All providers are registered in the DI container
- **Options Pattern Integration**: Each provider gets its own strongly-typed configuration via `IOptions<T>`
- **Dynamic Options Reload**: Provider options can be reloaded at runtime via `IOptionsSnapshot<T>`
- **Simplified Configuration**: Flat configuration structure
- **Automatic Provider Discovery**: Provider implementations are automatically discovered via reflection
- **Lightweight**: Minimal dependencies and smaller footprint

## Installation

```bash
dotnet add package Shared.DI.ProvidersConfig.Lite
```

## Quick Start

### 1. Define Your Contracts

```csharp
public interface IMessageProvider
{
    Task SendAsync(string message);
}
```

### 2. Create Service Using Providers

```csharp
public class MessageSender : IMessageSender, IHasProviders<IMessageProvider>
{
    private readonly IProviders<MessageSender, IMessageProvider> _providers;
    private readonly IProviderSwitcher<MessageSender, IMessageProvider> _switcher;

    public MessageSender(
        IProviders<MessageSender, IMessageProvider> providers,
        IProviderSwitcher<MessageSender, IMessageProvider> switcher)
    {
        _providers = providers;
        _switcher = switcher;
    }

    public async Task SendEmailAsync(string message)
    {
        // Select specific provider by key
        var emailProvider = _providers.Of("email");
        await emailProvider.SendAsync(message);
    }

    public async Task SendSmsAsync(string message)
    {
        // Select specific provider by key
        var smsProvider = _providers.Of("sms");
        await smsProvider.SendAsync(message);
    }

    public async Task SendUsingDefaultAsync(string message)
    {
        // Use current default provider
        var defaultProvider = _providers.Provider;
        await defaultProvider.SendAsync(message);
    }

    public void SwitchToEmail()
    {
        // Switch default provider at runtime
        _switcher.CurrentKey = "email";
    }

    public void SwitchToSms()
    {
        // Switch default provider at runtime
        _switcher.CurrentKey = "sms";
    }
}
```

### 3. Implement Providers

```csharp
public class EmailProviderOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
}

public class EmailProvider : IMessageProvider, IProvider<IMessageProvider, EmailProviderOptions>
{
    private readonly IOptionsSnapshot<EmailProviderOptions> _options;

    public EmailProvider(IOptionsSnapshot<EmailProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"Sending via SMTP {_options.Value.SmtpHost}:{_options.Value.SmtpPort}");
        return Task.CompletedTask;
    }
}
```

### 4. Configure in appsettings.json

```json
{
  "Providers": {                          // Configuration section name
    "Default": "email",                   // Default provider key for _providers.Provider
    "email": {                            // Provider key for _providers.Of("email")
      "Type": "EmailProvider",            // Provider class name
      "Description": "Email provider using SMTP",  // Optional description
      "Configuration": {                  // Options bound to EmailProviderOptions
        "SmtpHost": "smtp.example.com",
        "SmtpPort": 587
      }
    },
    "sms": {                              // Provider key for _providers.Of("sms")
      "Type": "SmsProvider",              // Provider class name
      "Description": "SMS provider using REST API",
      "Configuration": {                  // Options bound to SmsProviderOptions
        "ApiKey": "your-api-key-here",
        "ApiUrl": "https://api.sms-provider.com"
      }
    }
  }
}
```

**Configuration Structure:**
- `Default`: Default provider key (used by `_providers.Provider`)
- Each provider has a string key (e.g., "email", "sms")
  - `Type`: Provider class name (simple name or full name)
  - `Description`: Optional description of the provider
  - `Configuration`: Provider-specific options bound to options class

### 5. Register in DI

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddProvidersConfig<IMessageSender, MessageSender, IMessageProvider>(
    configuration,
    ServiceLifetime.Scoped,
    "Providers");
```

### 6. Use It

```csharp
var messageSender = serviceProvider.GetRequiredService<IMessageSender>();

// Use specific provider
await messageSender.SendEmailAsync("Hello from Email!");
await messageSender.SendSmsAsync("Hello from SMS!");

// Use default provider (configured as "email")
await messageSender.SendUsingDefaultAsync("Hello from Default!");

// Switch default provider at runtime
messageSender.SwitchToSms();
await messageSender.SendUsingDefaultAsync("Now using SMS as default!");
```

## Runtime Provider Switching

Switch the default provider at runtime using `IProviderSwitcher`:

```csharp
public class MessageSender : IMessageSender, IHasProviders<IMessageProvider>
{
    private readonly IProviders<MessageSender, IMessageProvider> _providers;
    private readonly IProviderSwitcher<MessageSender, IMessageProvider> _switcher;

    public MessageSender(
        IProviders<MessageSender, IMessageProvider> providers,
        IProviderSwitcher<MessageSender, IMessageProvider> switcher)
    {
        _providers = providers;
        _switcher = switcher;
    }

    public async Task SendUsingDefaultAsync(string message)
    {
        // Uses provider specified by _switcher.CurrentKey
        var defaultProvider = _providers.Provider;
        await defaultProvider.SendAsync(message);
    }

    public void SwitchToEmail()
    {
        _switcher.CurrentKey = "email";
    }

    public void SwitchToSms()
    {
        _switcher.CurrentKey = "sms";
    }
}

// Usage
await messageSender.SendUsingDefaultAsync("Using email"); // Uses "email" (from config)
messageSender.SwitchToSms();
await messageSender.SendUsingDefaultAsync("Using SMS"); // Now uses "sms"
```

**Key Points:**
- `IProviderSwitcher.CurrentKey` controls which provider is returned by `_providers.Provider`
- Changes affect all subsequent calls to `_providers.Provider`
- Initial value comes from `Default` configuration field or first configured provider
- Switching does not require configuration changes or application restart
- Switcher is registered as Singleton, so changes are visible across all scopes

## Dynamic Configuration Reload

Provider options can be changed at runtime:

1. Edit `appsettings.json`:
   ```json
   "Items": {
     "email": {
       "Type": "EmailProvider",
       "Configuration": {
         "SmtpHost": "smtp.newhost.com",
         "SmtpPort": 465
       }
     }
   }
   ```

2. Save the file

3. Next provider resolution automatically uses the new configuration!

**Note:** Configuration reload works for provider options via `IOptionsSnapshot<T>`, but you cannot change which provider type is mapped to a key at runtime. Provider type mappings are fixed at startup.

## Capabilities

✅ **String-keyed provider selection** - Use simple string keys for provider identification

✅ **Runtime provider switching** - Change default provider at runtime via `IProviderSwitcher`

✅ **Default provider support** - Access current default provider via `_providers.Provider`

✅ **Multiple instances of same provider** - Use same provider class with different keys and configurations

✅ **Automatic provider discovery** - Providers are discovered via reflection at startup

✅ **DI container registration** - All providers are registered in the DI container with keyed services

✅ **Options pattern integration** - Each provider has strongly-typed configuration

✅ **Dynamic options reload** - Provider options reload at runtime via `IOptionsSnapshot<T>`

✅ **Simple configuration structure** - Flat configuration structure

✅ **Lightweight dependencies** - Minimal package dependencies

✅ **Provider descriptions** - Optional descriptions for documentation

## Limitations

❌ **Provider type mappings fixed at startup** - Cannot change which provider type is mapped to a key at runtime

❌ **No assembly type caching** - No configurable cache lifetime for type scanning

❌ **Cannot add/remove providers without restart** - Provider discovery happens at startup only

## How It Works

1. **Registration Phase:**
   - All types implementing `TRealProviderInterface` are discovered via reflection
   - Providers are registered in DI container with specified lifetime
   - `IProviders<THasProviders, TRealProviderInterface>` is registered
   - Holder class (e.g., `MessageSender`) is registered as interface
   - Provider options are bound from configuration

2. **Resolution Phase:**
   - When you call `Of(stringKey)`, it retrieves the provider from DI container
   - Provider options are resolved via `IOptionsSnapshot<T>` for dynamic reload support

3. **Configuration Reload:**
   - When configuration changes, `IOptionsSnapshot<T>` automatically picks up new values
   - Next provider resolution uses updated configuration

## Advanced Examples

### Example 1: Multiple Instances of Same Provider Class

You can register multiple instances of the same provider class with different keys and configurations:

```csharp
// Configuration (appsettings.json)
{
  "Providers": {
    "Default": "primary-sms",
    "primary-sms": {
      "Type": "SmsProvider",
      "Description": "Primary SMS provider",
      "Configuration": {
        "ApiKey": "primary-api-key",
        "ApiUrl": "https://api.primary-sms.com"
      }
    },
    "backup-sms": {
      "Type": "SmsProvider",
      "Description": "Backup SMS provider for failover",
      "Configuration": {
        "ApiKey": "backup-api-key",
        "ApiUrl": "https://api.backup-sms.com"
      }
    }
  }
}

// Usage
public class MessageSender : IMessageSender, IHasProviders<IMessageProvider>
{
    private readonly IProviders<MessageSender, IMessageProvider> _providers;

    public MessageSender(IProviders<MessageSender, IMessageProvider> providers)
    {
        _providers = providers;
    }

    public async Task SendWithFailoverAsync(string message)
    {
        try
        {
            var primary = _providers.Of("primary-sms");
            await primary.SendAsync(message);
        }
        catch (Exception)
        {
            var backup = _providers.Of("backup-sms");
            await backup.SendAsync(message);
        }
    }
}
```

Each key gets its own instance of `SmsProvider` with different configuration via `IOptionsSnapshot<T>.Get(key)`.

### Example 2: Failover with Runtime Switching

```csharp
public class MessageSender : IMessageSender, IHasProviders<IMessageProvider>
{
    private readonly IProviders<MessageSender, IMessageProvider> _providers;
    private readonly IProviderSwitcher<MessageSender, IMessageProvider> _switcher;

    public MessageSender(
        IProviders<MessageSender, IMessageProvider> providers,
        IProviderSwitcher<MessageSender, IMessageProvider> switcher)
    {
        _providers = providers;
        _switcher = switcher;
    }

    public async Task SendWithFailoverAsync(string message)
    {
        try
        {
            // Try default provider
            await _providers.Provider.SendAsync(message);
        }
        catch (Exception)
        {
            // Switch to backup provider
            _switcher.CurrentKey = "backup-sms";
            await _providers.Provider.SendAsync(message);
        }
    }
}
```

### Example 3: Provider Information

```csharp
// Access provider metadata
var providers = serviceProvider.GetRequiredService<IProviders<MessageSender, IMessageProvider>>();

foreach (var (key, info) in providers.Providers)
{
    Console.WriteLine($"Provider: {key}");
    Console.WriteLine($"Description: {info.Description}");
}
```

## Examples

See [Shared.DI.ProvidersConfig.Lite.Example](../Shared.DI.ProvidersConfig.Lite.Example/EXAMPLES.md) for detailed examples including:
- Multiple providers with string keys
- Runtime provider switching with `IProviderSwitcher`
- Default provider usage with `_providers.Provider`
- Dynamic configuration reload
- Project structure best practices

## Best Practices

1. **Use descriptive string keys** for provider identification (e.g., "email", "sms", "primary-db")
2. **Configure default provider** in `Default` field for `_providers.Provider` usage
3. **Use `IProviderSwitcher`** for runtime provider switching instead of configuration changes
4. **Set `reloadOnChange: true`** in ConfigurationBuilder for dynamic options reload
5. **Use `IOptionsSnapshot<T>`** in providers to support configuration reload
6. **Organize code** by separating Contracts, Providers, and Services
7. **Choose appropriate lifetime** for your use case (Scoped recommended)
8. **Add descriptions** to providers in configuration for documentation
9. **Use consistent naming** for provider keys across your application

## License

MIT
