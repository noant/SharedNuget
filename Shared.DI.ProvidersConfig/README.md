# Shared.DI.ProvidersConfig

Configuration-driven provider selection with DI integration and dynamic reload support.

## Features

- **Automatic Provider Discovery**: All provider implementations are automatically discovered and registered via reflection
- **Dynamic Configuration Reload**: Change active providers in `appsettings.json` without restarting the application
- **Configuration-Based Selection**: Select providers through JSON configuration
- **Options Pattern Integration**: Each provider gets its own strongly-typed configuration via `IOptions<T>`
- **Zero Manual Registration**: No need to manually register provider implementations in DI
- **Multiple Registration Methods**: Register via concrete type or interface
- **Runtime Provider Selection**: Switch between providers at runtime or inject single provider directly

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
    private readonly EmailProviderOptions _options;

    public EmailProvider(IOptions<EmailProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task SendAsync(string message)
    {
        Console.WriteLine($"Sending via SMTP {_options.SmtpHost}:{_options.SmtpPort}");
        return Task.CompletedTask;
    }
}
```

### 4. Configure in appsettings.json

```json
{
  "providersConfiguration": {
    "MessageSender": {
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
          "ApiKey": "your-api-key",
          "ApiUrl": "https://api.sms-provider.com"
        }
      }
    }
  }
}
```

### 5. Register in DI

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<MessageSender>();
```

### 6. Use It

```csharp
var messageSender = serviceProvider.GetRequiredService<MessageSender>();
await messageSender.SendEmailAsync("Hello!");
```

## Dynamic Configuration Reload

Change active providers while the application is running:

1. Edit `appsettings.json`:
   ```json
   "activeProviders": {
     "Email": "EmailProvider",
     "Sms": "SecondarySmsProvider"  // Changed from SmsProvider
   }
   ```

2. Save the file

3. Next provider resolution automatically uses the new provider!

**Requirements:**
- `reloadOnChange: true` in ConfigurationBuilder
- Use Scoped or Transient lifetime for services

## Registration Methods

### Option 1: Via Concrete Type

```csharp
services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<MessageSender>();
```

### Option 2: Via Interface

```csharp
services.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<IMessageSender, MessageSender>();
```

## How It Works

1. **Registration Phase:**
   - All provider types implementing `IMessageProvider` are discovered via reflection
   - All providers are registered in DI (not just active ones)
   - Options are configured for each provider
   - `SimpleProviders` is registered with access to all providers

2. **Resolution Phase:**
   - When you resolve `IProviders<TEnum, TProvider>`, it gets all registered providers
   - On each property/method access, it reads current configuration via `IOptionsMonitor<T>`
   - Filters providers based on current `activeProviders` configuration
   - Returns filtered provider(s)

## Examples

See [EXAMPLES.md](./EXAMPLES.md) for detailed examples including:
- Multiple providers with runtime selection
- Dynamic provider switching
- Project structure best practices

## License

MIT
