# Shared.DI.ProvidersConfig Examples

## Overview

This library provides automatic provider registration with dynamic configuration reload support. Features:
- Automatic provider discovery and registration
- Runtime configuration changes without restart
- Support for multiple provider types per service
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
    private readonly EmailProviderOptions _options;

    public EmailProvider(IOptions<EmailProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[EmailProvider] Sending via SMTP {_options.SmtpHost}:{_options.SmtpPort}");
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
    private readonly SmsProviderOptions _options;

    public SmsProvider(IOptions<SmsProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"[SmsProvider] Sending via API {_options.ApiUrl}");
        Console.WriteLine($"[SmsProvider] API Key: {_options.ApiKey}");
        Console.WriteLine($"[SmsProvider] Message: {message}");
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
      "defaultProvider": "Email",
      "activeProviders": {
        "Email": "EmailProvider",
        "Sms": "SmsProvider"
      },
      "configurations": {
        "EmailProvider": {
          "SmtpHost": "smtp.example.com",
          "SmtpPort": 587
        },
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
```

### Step 5: Register Services

#### Option 1: Registration via Concrete Type

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
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

#### Option 2: Registration via Interface

```csharp
services.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<IMessageSender, MessageSender>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
    
    await messageSender.SendEmailAsync("Hello!");
    await messageSender.SendSmsAsync("Hello!");
}
```

## Dynamic Configuration Reload

The library supports runtime configuration changes without application restart:

1. Start the application with initial configuration
2. While running, edit `appsettings.json`:
   ```json
   "defaultProvider": "Sms",
   "activeProviders": {
     "Email": "EmailProvider",
     "Sms": "SecondarySmsProvider"  // Changed from SmsProvider
   }
   ```
3. Save the file
4. Next service resolution will use the new provider automatically

**Note:** Configuration reload works because:
- `reloadOnChange: true` in ConfigurationBuilder
- `IOptionsMonitor<T>` tracks configuration changes
- Providers are resolved fresh on each request

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
All types implementing `IMessageProvider` are automatically discovered and registered in DI.

### Runtime Provider Selection
Change active providers in configuration file without restarting the application.

### Options Pattern Integration
Each provider can have its own strongly-typed configuration class.

### Multiple Registration Methods
- Register via concrete type: `AddProvidersConfiguration<MessageSender>`
- Register via interface: `AddProvidersConfiguration<IMessageSender, MessageSender>`

## Best Practices

1. **Use `IOptionsMonitor<T>`** in providers that need configuration reload support
2. **Use `IOptions<T>`** in providers with static configuration
3. **Set `reloadOnChange: true`** in ConfigurationBuilder for dynamic reload
4. **Use Scoped lifetime** for services that need fresh provider resolution
5. **Organize code** by separating Contracts, Providers, and Services
