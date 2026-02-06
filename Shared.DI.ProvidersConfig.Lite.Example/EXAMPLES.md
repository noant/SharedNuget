# Shared.DI.ProvidersConfig.Lite Examples

## Overview

This library provides a simplified provider configuration system with string-based keys. Features:
- String-keyed provider selection (no enum required)
- Runtime provider switching via `IProviderSwitcher`
- Default provider support via `_providers.Provider`
- Providers registered in DI container
- Dynamic configuration reload per provider via `IOptionsSnapshot<T>` pattern
- Simple flat configuration structure
- No assembly type caching complexity
- Lightweight with minimal dependencies

## Project Structure

```
Shared.DI.ProvidersConfig.Lite.Examples/
├── Contracts/              # Interfaces
│   ├── IMessageProvider.cs
│   └── IMessageSender.cs
├── Providers/              # Provider implementations
│   ├── EmailProvider.cs
│   ├── SmsProvider.cs
│   └── SecondarySmsProvider.cs
├── Services/               # Services using providers
│   └── MessageSender.cs
├── Program.cs
└── appsettings.json
```

## Example: Multiple Providers with String Keys

### Step 1: Define Contracts

```csharp
// IMessageProvider.cs
public interface IMessageProvider
{
    Task SendAsync(string message);
}

// IMessageSender.cs
public interface IMessageSender
{
    Task SendEmailAsync(string message);
    Task SendSmsAsync(string message);
    Task SendUsingDefaultAsync(string message);
    void SwitchToEmail();
    void SwitchToSms();
}
```

### Step 2: Implement Providers

```csharp
// EmailProvider.cs
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
        Console.WriteLine($"[EmailProvider] Sending via SMTP {_options.Value.SmtpHost}:{_options.Value.SmtpPort}");
        Console.WriteLine($"[EmailProvider] Message: {message}");
        return Task.CompletedTask;
    }
}

// SmsProvider.cs
public class SmsProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}

public class SmsProvider : IMessageProvider, IProvider<IMessageProvider, SmsProviderOptions>
{
    private readonly IOptionsSnapshot<SmsProviderOptions> _options;

    public SmsProvider(IOptionsSnapshot<SmsProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[SmsProvider] Sending via API {_options.Value.ApiUrl}");
        Console.WriteLine($"[SmsProvider] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[SmsProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
```

### Step 3: Create Service

```csharp
// MessageSender.cs
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
        var emailProvider = _providers.Of("email");
        await emailProvider.SendAsync(message);
    }

    public async Task SendSmsAsync(string message)
    {
        var smsProvider = _providers.Of("sms");
        await smsProvider.SendAsync(message);
    }

    public async Task SendUsingDefaultAsync(string message)
    {
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
```

### Step 4: Configure (appsettings.json)

```json
{
  "Providers": {
    "Default": "email",
    "email": {
      "Type": "EmailProvider",
      "Description": "Email provider using SMTP",
      "Configuration": {
        "SmtpHost": "smtp.example.com",
        "SmtpPort": 587
      }
    },
    "sms": {
      "Type": "SmsProvider",
      "Description": "SMS provider using REST API",
      "Configuration": {
        "ApiKey": "your-api-key-here",
        "ApiUrl": "https://api.sms-provider.com"
      }
    },
    "secondary-sms": {
      "Type": "SecondarySmsProvider",
      "Description": "Secondary SMS provider for failover",
      "Configuration": {
        "ApiKey": "secondary-api-key",
        "ApiUrl": "https://api.secondary-sms.com",
        "ProviderName": "Secondary SMS Service"
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

### Step 5: Register Services

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

// Register providers configuration
services.AddProvidersConfig<IMessageSender, MessageSender, IMessageProvider>(
    configuration,
    ServiceLifetime.Scoped,
    "Providers");

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
    
    await messageSender.SendEmailAsync("Hello from Email Provider!");
    await messageSender.SendSmsAsync("Hello from SMS Provider!");
    await messageSender.SendUsingDefaultAsync("Hello from Default Provider!");
}
```

## Runtime Provider Switching

The library supports runtime provider switching via `IProviderSwitcher`:

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
- Initial value comes from `Default` configuration field or first configured provider
- Changes affect all subsequent calls to `_providers.Provider`
- Switching does not require configuration changes or application restart
- Switcher is registered as Singleton, so changes are visible across all scopes

## Dynamic Configuration Reload

The library supports runtime configuration changes for provider options (but not provider key mappings):

1. Start the application with initial configuration
2. While running, edit `appsettings.json`:
   ```json
   "email": {
     "Type": "EmailProvider",
     "Configuration": {
       "SmtpHost": "smtp.newhost.com",
       "SmtpPort": 465
     }
   }
   ```
3. Save the file
4. Next provider resolution will use the new configuration automatically

**Note:** Configuration reload works for provider options via `IOptionsSnapshot<T>`, but you cannot change which provider type is mapped to a key at runtime. The provider type mappings are determined at startup.

## Key Features

### String-Based Provider Keys
Use simple string keys instead of enums for provider selection.

### Runtime Provider Switching
Change default provider at runtime via `IProviderSwitcher` without configuration changes or restart.

### Default Provider Support
Access current default provider via `_providers.Provider` property.

### DI Registration
All providers are registered in the DI container during startup.

### Options Pattern Integration
Each provider can have its own strongly-typed configuration class, bound from configuration section.

### Simplified Configuration
Flat configuration structure with string keys - no enum-keyed hierarchy.

### Dynamic Options Reload
Provider options can be reloaded at runtime via `IOptionsSnapshot<T>` pattern.

## Capabilities

- ✅ String-keyed provider selection
- ✅ Runtime provider switching via `IProviderSwitcher`
- ✅ Default provider support via `_providers.Provider`
- ✅ Multiple instances of same provider class with different keys and configurations
- ✅ Automatic provider discovery via reflection
- ✅ Providers registered in DI container with keyed services
- ✅ Options pattern integration for each provider
- ✅ Dynamic configuration reload for provider options
- ✅ Simple flat configuration structure
- ✅ Lightweight with minimal dependencies
- ✅ Support for provider descriptions

## Limitations

- ❌ No enum-based provider selection
- ❌ Provider type mappings are fixed at startup (cannot change which provider type is mapped to a key)
- ❌ No assembly type caching or configurable cache lifetime

## Comparison with Full Version

| Feature | Lite | Full |
|---------|------|------|
| Provider keys | String | Enum |
| Provider registration | DI container (keyed services) | Manual construction |
| Configuration structure | Flat | Enum-keyed hierarchy |
| Dynamic options reload | Yes | Yes |
| Runtime provider switching | Yes (IProviderSwitcher) | Yes (IProviderSwitcher) |
| Default provider | Yes | Yes |
| Assembly type caching | No | Yes (configurable) |
| Multiple instances of same provider | Yes | Yes |
| Dependencies | Lighter | More |

## Best Practices

1. **Use descriptive string keys** for provider identification
2. **Set `reloadOnChange: true`** in ConfigurationBuilder for dynamic reload
3. **Use `IOptionsSnapshot<T>`** in providers to support configuration reload
4. **Organize code** by separating Contracts, Providers, and Services
5. **Choose appropriate lifetime** for your use case (Scoped recommended)
6. **Add descriptions** to providers in configuration for documentation

## Advanced Examples

### Example: Multiple Instances of Same Provider Class

You can register multiple instances of the same provider class with different keys and configurations:

```json
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
    },
    "emergency-sms": {
      "Type": "SmsProvider",
      "Description": "Emergency SMS provider",
      "Configuration": {
        "ApiKey": "emergency-api-key",
        "ApiUrl": "https://api.emergency-sms.com"
      }
    }
  }
}
```

Each key will get its own instance of `SmsProvider` with different configuration via `IOptionsSnapshot<T>.Get(key)`:

```csharp
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
            try
            {
                var backup = _providers.Of("backup-sms");
                await backup.SendAsync(message);
            }
            catch (Exception)
            {
                var emergency = _providers.Of("emergency-sms");
                await emergency.SendAsync(message);
            }
        }
    }
}
```

## When to Use Lite vs Full

**Use Lite when:**
- You need simple string-based provider selection
- You want lighter dependencies
- Configuration reload for options is sufficient
- Runtime provider switching via `IProviderSwitcher` is sufficient
- Keyed services registration in DI is acceptable

**Use Full when:**
- You need enum-based type-safe provider selection
- You need assembly type caching for performance
- You need more complex provider management scenarios
- You prefer manual provider construction over DI container resolution
