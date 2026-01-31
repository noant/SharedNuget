# Shared.DI.ProvidersConfig Examples

## Overview

This library provides automatic provider discovery with manual construction and dynamic configuration reload support. Features:
- Automatic provider discovery via reflection (no DI registration)
- Manual provider construction without DI container pollution
- Enum-keyed hierarchical configuration structure
- Runtime configuration changes without restart
- Support for multiple instances of same provider class
- Assembly type caching with configurable lifetime
- Options pattern integration for each provider

## Project Structure

```
Shared.DI.ProvidersConfig.Example/
├── Contracts/              # Interfaces and enums
│   ├── IMessageProvider.cs
│   ├── IMessageSender.cs
│   └── MessageType.cs
├── Providers/              # Provider implementations
│   ├── EmailProvider.cs
│   ├── SmsProvider.cs
│   └── SecondarySmsProvider.cs
├── Services/               # Services using providers
│   └── MessageSender.cs
├── Program.cs
└── appsettings.json
```

## Example: Multiple Providers with Runtime Selection

### Step 1: Define Contracts

```csharp
// MessageType.cs
public enum MessageType
{
    Email,
    Sms
}

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

// UniversalApiProvider.cs - Universal provider for both Email and SMS
public class UniversalApiProviderOptions
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
}

public class UniversalApiProvider : IMessageProvider, IProvider<IMessageProvider, UniversalApiProviderOptions>
{
    private readonly IOptionsSnapshot<UniversalApiProviderOptions> _options;

    public UniversalApiProvider(IOptionsSnapshot<UniversalApiProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[UniversalApiProvider] Sending via Universal API {_options.Value.ApiUrl}");
        Console.WriteLine($"[UniversalApiProvider] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[UniversalApiProvider] Message Type: {_options.Value.MessageType}");
        Console.WriteLine($"[UniversalApiProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
```

### Step 3: Create Service

```csharp
// MessageSender.cs
public class MessageSender : IMessageSender, IHasProviders<MessageType, IMessageProvider>
{
    private readonly IProviders<MessageType, IMessageProvider> _providers;

    public MessageSender(IProviders<MessageType, IMessageProvider> providers)
    {
        _providers = providers;
    }

    public async Task SendEmailAsync(string message)
    {
        var emailProvider = _providers.Of(MessageType.Email);
        await emailProvider.SendAsync(message);
    }

    public async Task SendSmsAsync(string message)
    {
        var smsProvider = _providers.Of(MessageType.Sms);
        await smsProvider.SendAsync(message);
    }

    public async Task SendUsingDefaultAsync(string message)
    {
        var defaultProvider = _providers.Provider;
        await defaultProvider.SendAsync(message);
    }
}
```

### Step 4: Configure (appsettings.json)

```json
{
  "providersConfiguration": {
    "MessageSender": {
      "cacheLifetime": "00:00:15",
      "reloadAssemblyInfo": false,
      "defaultProvider": "Email",
      "activeProviders": {
        "Email": "UniversalApiProvider",
        "Sms": "UniversalApiProvider"
      },
      "configurations": {
        "Email": {
          "EmailProvider": {
            "SmtpHost": "smtp.example.com",
            "SmtpPort": 587
          },
          "UniversalApiProvider": {
            "ApiUrl": "https://api.universal-provider.com/v1",
            "ApiKey": "universal-email-api-key",
            "MessageType": "SendEmail"
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
          },
          "UniversalApiProvider": {
            "ApiUrl": "https://api.universal-provider.com/v1",
            "ApiKey": "universal-sms-api-key",
            "MessageType": "SendSms"
          }
        }
      }
    }
  }
}
```

**Note:** In this example, `UniversalApiProvider` is used for both `Email` and `Sms` enum keys. The same provider class is instantiated twice with different configurations:
- For `Email`: `MessageType: "SendEmail"` with `universal-email-api-key`
- For `Sms`: `MessageType: "SendSms"` with `universal-sms-api-key`

This demonstrates the power of enum-keyed configuration - the same provider class can be reused with different configurations for different purposes.

**Configuration Fields:**
- `cacheLifetime` (optional, default: `00:00:15`): TimeSpan for assembly type cache expiration
- `reloadAssemblyInfo` (optional, default: `false`): If `false`, assembly type cache never expires after first load
- `defaultProvider` (required): Default enum key for provider selection
- `activeProviders` (required): Dictionary mapping enum keys to provider class names
- `configurations` (required): Nested dictionary: `{EnumKey}.{ProviderClassName}.{ProviderOptions}`

### Step 5: Register Services

#### Option 1: Registration via Concrete Type (Recommended: Singleton)

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

// Recommended: Register as Singleton for optimal caching performance
services.AddProvidersConfiguration<MessageSender>(configuration);
services.AddScoped<MessageSender>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var messageSender = scope.ServiceProvider.GetRequiredService<MessageSender>();
    
    await messageSender.SendEmailAsync("Hello from Email Provider!");
    await messageSender.SendSmsAsync("Hello from SMS Provider!");
    await messageSender.SendUsingDefaultAsync("Hello from Default Provider!");
}
```

#### Option 2: Registration via Interface (Recommended: Singleton)

```csharp
services.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration);
services.AddScoped<IMessageSender, MessageSender>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
    
    await messageSender.SendEmailAsync("Hello!");
    await messageSender.SendSmsAsync("Hello!");
}
```

**Note:** `AddProvidersConfiguration` defaults to `ServiceLifetime.Singleton` for optimal caching performance. Assembly type scanning and provider dictionary caching work best with Singleton registration.

## Dynamic Configuration Reload

The library supports runtime configuration changes without application restart:

1. Start the application with initial configuration
2. While running, edit `appsettings.json`:
   ```json
   "defaultProvider": "Sms",
   "activeProviders": {
     "Email": "EmailProvider",
     "Sms": "SecondarySmsProvider"
   },
   "configurations": {
     "Email": {
       "EmailProvider": {
         "SmtpHost": "smtp.newhost.com",
         "SmtpPort": 465
       }
     },
     "Sms": {
       "SecondarySmsProvider": {
         "ApiKey": "new-api-key",
         "ApiUrl": "https://api.new-sms.com"
       }
     }
   }
   ```
3. Save the file
4. Next provider resolution will use the new provider and configuration automatically

**Note:** Configuration reload works because:
- `reloadOnChange: true` in ConfigurationBuilder
- `IOptionsMonitor<T>` tracks configuration changes for provider selection
- Providers are constructed fresh on each `Of(enumKey)` call with current configuration

## Universal Provider Example

The library supports using the same provider class for multiple enum keys with different configurations. This is useful when you have a universal API that can handle different message types.

### UniversalApiProvider Implementation

```csharp
public class UniversalApiProviderOptions
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
}

public class UniversalApiProvider : IMessageProvider, IProvider<IMessageProvider, UniversalApiProviderOptions>
{
    private readonly IOptionsSnapshot<UniversalApiProviderOptions> _options;

    public UniversalApiProvider(IOptionsSnapshot<UniversalApiProviderOptions> options)
    {
        _options = options;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[UniversalApiProvider] Sending via Universal API {_options.Value.ApiUrl}");
        Console.WriteLine($"[UniversalApiProvider] API Key: {_options.Value.ApiKey}");
        Console.WriteLine($"[UniversalApiProvider] Message Type: {_options.Value.MessageType}");
        Console.WriteLine($"[UniversalApiProvider] Message: {message}");
        return Task.CompletedTask;
    }
}
```

### Configuration for Multiple Instances

```json
{
  "providersConfiguration": {
    "MessageSender": {
      "activeProviders": {
        "Email": "UniversalApiProvider",
        "Sms": "UniversalApiProvider"
      },
      "configurations": {
        "Email": {
          "UniversalApiProvider": {
            "ApiUrl": "https://api.universal-provider.com/v1",
            "ApiKey": "universal-email-api-key",
            "MessageType": "SendEmail"
          }
        },
        "Sms": {
          "UniversalApiProvider": {
            "ApiUrl": "https://api.universal-provider.com/v1",
            "ApiKey": "universal-sms-api-key",
            "MessageType": "SendSms"
          }
        }
      }
    }
  }
}
```

### Usage

```csharp
var providers = serviceProvider.GetRequiredService<IProviders<MessageType, IMessageProvider>>();

// Creates UniversalApiProvider instance with Email configuration
var emailProvider = providers.Of(MessageType.Email);
await emailProvider.SendAsync("Hello via Email!");
// Output: Message Type: SendEmail

// Creates UniversalApiProvider instance with SMS configuration
var smsProvider = providers.Of(MessageType.Sms);
await smsProvider.SendAsync("Hello via SMS!");
// Output: Message Type: SendSms
```

**Key Points:**
- Same provider class (`UniversalApiProvider`) is instantiated twice
- Each instance has different configuration based on enum key
- Configurations differ in `MessageType` field (`SendEmail` vs `SendSms`)
- Different API keys can be used for different message types
- This pattern is useful for universal APIs that support multiple operations

## Default Provider and Runtime Switching

### Default Provider Configuration

The `defaultProvider` field specifies which provider is used by `_providers.Provider`:

```json
{
  "providersConfiguration": {
    "MessageSender": {
      "defaultProvider": "Email",
      "activeProviders": {
        "Email": "EmailProvider",
        "Sms": "SmsProvider"
      }
    }
  }
}
```

If `defaultProvider` is not specified, the first active provider is used.

### Runtime Provider Switching

Use `IProviderSwitcher` to change the default provider at runtime:

```csharp
public class MessageSender : IMessageSender, IHasProviders<MessageType, IMessageProvider>
{
    private readonly IProviders<MessageType, IMessageProvider> _providers;
    private readonly IProviderSwitcher<IProviders<MessageType, IMessageProvider>, MessageType, IMessageProvider> _switcher;

    public MessageSender(
        IProviders<MessageType, IMessageProvider> providers,
        IProviderSwitcher<IProviders<MessageType, IMessageProvider>, MessageType, IMessageProvider> switcher)
    {
        _providers = providers;
        _switcher = switcher;
    }

    public void SwitchToSms()
    {
        _switcher.Current = MessageType.Sms;
    }

    public void SwitchToEmail()
    {
        _switcher.Current = MessageType.Email;
    }

    public async Task SendUsingDefaultAsync(string message)
    {
        // Uses provider specified by _switcher.Current
        var defaultProvider = _providers.Provider;
        await defaultProvider.SendAsync(message);
    }
}
```

**Key Points:**
- `IProviderSwitcher.Current` is thread-safe
- Changes affect all subsequent calls to `_providers.Provider`
- Initial value comes from `defaultProvider` configuration or first active provider
- Switching does not require configuration changes or application restart

## Key Features

### Automatic Provider Discovery
All types implementing `IMessageProvider` are automatically discovered via reflection (not registered in DI).

### Manual Provider Construction
Providers are constructed manually without DI container pollution, supporting multiple instances of same class.

### Enum-Keyed Configuration
Hierarchical configuration structure with enum keys allows multiple instances of same provider class with different configurations.

### Assembly Type Caching
Configurable caching of assembly scanning results for optimal performance.

### Runtime Provider Selection
Change active providers in configuration file without restarting the application.

### Options Pattern Integration
Each provider can have its own strongly-typed configuration class, bound from enum-keyed configuration section.

### Multiple Registration Methods
- Register via concrete type: `AddProvidersConfiguration<MessageSender>`
- Register via interface: `AddProvidersConfiguration<IMessageSender, MessageSender>`

## Best Practices

1. **Use Singleton lifetime** for `AddProvidersConfiguration` (default) for optimal caching performance
2. **Set `reloadOnChange: true`** in ConfigurationBuilder for dynamic reload
3. **Configure `cacheLifetime`** based on your assembly loading patterns (default: 15 seconds)
4. **Set `reloadAssemblyInfo: false`** (default) if assemblies don't change at runtime for best performance
5. **Organize code** by separating Contracts, Providers, and Services
6. **Use enum-keyed configuration** to support multiple instances of same provider class
