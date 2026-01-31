# Shared.DI.ProvidersConfig

Configuration-driven provider selection with DI integration and dynamic reload support.

## Features

- **Automatic Provider Discovery**: All provider implementations are automatically discovered via reflection
- **Dynamic Configuration Reload**: Change active providers in `appsettings.json` without restarting the application
- **Configuration-Based Selection**: Select providers through JSON configuration with enum-keyed hierarchy
- **Options Pattern Integration**: Each provider gets its own strongly-typed configuration via `IOptions<T>`
- **Manual Provider Construction**: Providers are constructed manually without DI registration pollution
- **Assembly Type Caching**: Configurable caching of assembly scanning results for optimal performance
- **Multiple Instances Support**: Same provider class can be instantiated multiple times with different enum keys and configurations
- **Multiple Registration Methods**: Register via concrete type or interface
- **Runtime Provider Selection**: Switch between providers at runtime or inject single provider directly
- **Recommended Singleton Lifetime**: Optimal caching performance with Singleton registration (default)

## Installation

```bash
dotnet add package Shared.DI.ProvidersConfig
```

## Quick Start

### 1. Define Your Contracts

```csharp
public enum MessageType { Email, Sms }

public interface IMessageProvider
{
    Task SendAsync(string message);
}
```

### 2. Create Service Using Providers

```csharp
public class MessageSender : IHasProviders<MessageType, IMessageProvider>
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
            "ApiKey": "your-api-key",
            "ApiUrl": "https://api.sms-provider.com"
          }
        }
      }
    }
  }
}
```

**Configuration Fields:**
- `cacheLifetime` (optional, default: `00:00:15`): TimeSpan for assembly type cache expiration
- `reloadAssemblyInfo` (optional, default: `false`): If `false`, assembly type cache never expires after first load
- `defaultProvider` (required): Default enum key for provider selection
- `activeProviders` (required): Dictionary mapping enum keys to provider class names
- `configurations` (required): Nested dictionary: `{EnumKey}.{ProviderClassName}.{ProviderOptions}`

### 5. Register in DI

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Recommended: Register as Singleton for optimal caching performance
services.AddProvidersConfiguration<MessageSender>(configuration);
```

**Note:** `AddProvidersConfiguration` automatically registers the holder class (`MessageSender`) in DI with the specified lifetime (defaults to `ServiceLifetime.Singleton` for optimal caching performance).

### 6. Use It

```csharp
var messageSender = serviceProvider.GetRequiredService<MessageSender>();
await messageSender.SendEmailAsync("Hello!");
```

## Dynamic Configuration Reload

Change active providers and their configurations while the application is running:

1. Edit `appsettings.json`:
   ```json
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

2. Save the file

3. Next provider resolution automatically uses the new provider and updated configuration!

**Requirements:**
- `reloadOnChange: true` in ConfigurationBuilder
- Providers are constructed fresh on each resolution, so configuration changes are immediately reflected

## Multiple Instances of Same Provider

The enum-keyed configuration structure supports multiple instances of the same provider class with different configurations:

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
          }
        }
      }
    }
  }
}
```

In this example, `OpenAiLlmProvider` is instantiated twice with different configurations for `Chat` and `Reasoner` enum keys.

## Registration Methods

### Option 1: Via Concrete Type (Recommended: Singleton)

```csharp
services.AddProvidersConfiguration<MessageSender>(configuration);
```

### Option 2: Via Interface (Recommended: Singleton)

```csharp
services.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration);
```

### Custom Lifetime (Not Recommended)

```csharp
services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
```

**Note:** 
- `AddProvidersConfiguration` automatically registers the holder class in DI with the specified lifetime
- Singleton lifetime (default) is recommended for optimal caching performance
- Assembly type scanning and provider dictionary caching work best with Singleton registration

## How It Works

1. **Registration Phase:**
   - `SimpleProvidersOptions` is configured from `appsettings.json`
   - `IProviders<TEnum, TProvider>` and `IProviderSwitcher` are registered in DI
   - Holder class (e.g., `MessageSender`) is automatically registered in DI
   - **No provider types are registered in DI** - they are constructed manually

2. **Resolution Phase:**
   - When you call `Of(enumKey)`, it:
     1. Scans all loaded assemblies for types implementing `TRealProvider` (cached with configurable lifetime)
     2. Reads current configuration via `IOptionsMonitor<T>`
     3. Constructs provider instance manually by resolving constructor parameters from DI
     4. For `IOptions<T>` parameters, binds configuration from `configurations.{EnumKey}.{ProviderClassName}`
     5. Returns constructed provider instance

3. **Caching:**
   - **Assembly type scanning** is cached based on `cacheLifetime` and `reloadAssemblyInfo` settings
   - **Provider dictionary** (enum-to-type mapping) is cached based on `cacheLifetime`
   - **Reflection metadata** (constructors, parameters) is cached permanently for performance

## Examples

See [EXAMPLES.md](./EXAMPLES.md) for detailed examples including:
- Multiple providers with runtime selection
- Dynamic provider switching
- Project structure best practices

## License

MIT
