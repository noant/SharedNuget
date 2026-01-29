# SharedNuget

Collection of reusable .NET libraries published as NuGet packages.

## Projects

### Shared.DI.ProvidersConfig

Library for configuration-driven provider selection with DI integration.

**Installation:**
```bash
dotnet add package Shared.DI.ProvidersConfig
```

**Key Features:**
- Automatic provider discovery and registration via reflection
- Configuration-based provider selection
- **Dynamic configuration reload without application restart**
- Automatic IOptions configuration for each provider
- No manual DI registration needed for provider implementations
- Support for runtime provider selection or single provider injection
- Two registration methods: via concrete type or interface

**Usage Example 1: Multiple Providers with Runtime Selection**

```csharp
// Define provider enum
public enum MessageType
{
    Email,
    Sms
}

// Define provider interface
public interface IMessageProvider
{
    Task SendAsync(string message);
}

// Define holder class
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
    
    public async Task SendDefaultAsync(string message)
    {
        await _providers.Provider.SendAsync(message);
    }
}

// Define provider implementation
public class EmailProviderOptions
{
    public string SmtpHost { get; set; }
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
        // Implementation
        return Task.CompletedTask;
    }
}

// Configuration (appsettings.json)
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
          "ApiKey": "your-api-key"
        }
      }
    }
  }
}

// DI Registration Option 1: Via concrete type
services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<MessageSender>();

// DI Registration Option 2: Via interface
services.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<IMessageSender, MessageSender>();

// Usage
var messageSender = serviceProvider.GetRequiredService<MessageSender>();
await messageSender.SendEmailAsync("Hello!");

// Dynamic Configuration Reload
// While app is running, edit appsettings.json:
// Change "Sms": "SmsProvider" to "Sms": "SecondarySmsProvider"
// Next resolution will use the new provider automatically!
```

**Usage Example 2: Dynamic Provider Switching**

```csharp
// Add multiple SMS providers
public class SmsProvider : IMessageProvider, IProvider<IMessageProvider, SmsProviderOptions> { }
public class SecondarySmsProvider : IMessageProvider, IProvider<IMessageProvider, SecondarySmsProviderOptions> { }

// Initial configuration
{
  "providersConfiguration": {
    "MessageSender": {
      "activeProviders": {
        "Email": "EmailProvider",
        "Sms": "SmsProvider"  // Using primary SMS provider
      },
      "configurations": {
        "SmsProvider": { "ApiKey": "primary-key", "ApiUrl": "https://api.primary.com" },
        "SecondarySmsProvider": { "ApiKey": "secondary-key", "ApiUrl": "https://api.secondary.com" }
      }
    }
  }
}

// While application is running, edit appsettings.json:
// Change "Sms": "SmsProvider" to "Sms": "SecondarySmsProvider"
// Save the file - next provider resolution will use SecondarySmsProvider!

// This works because:
// - ConfigurationBuilder has reloadOnChange: true
// - Library uses IOptionsMonitor<T> for dynamic configuration tracking
// - Providers are resolved fresh on each request
```

**Important Notes:**
- No need to manually register provider implementations in DI
- Library automatically discovers and registers **all** provider classes (not just active ones)
- Active providers are filtered dynamically based on current configuration
- IOptions<TOptions> is automatically configured for each provider from configurations section
- Configuration changes are picked up automatically without restart (use `reloadOnChange: true`)
- Use Scoped or Transient lifetime for services that need fresh provider resolution
- See `Shared.DI.ProvidersConfig.Example/EXAMPLES.md` for detailed examples

### Shared.Utils.NugetPublisher

Console application for automated NuGet package publishing with version increment.

**Usage:**
```bash
dotnet run --project Shared.Utils.NugetPublisher -- \
  --project Shared.DI.ProvidersConfig \
  --api-key YOUR_NUGET_API_KEY

# With custom source
dotnet run --project Shared.Utils.NugetPublisher -- \
  -p Shared.DI.ProvidersConfig \
  -k YOUR_NUGET_API_KEY \
  -s https://custom.nuget.org/v3/index.json
```

**Features:**
- Automatically queries NuGet.org for latest version
- Increments patch version (1.0.x pattern)
- Builds package with `dotnet pack`
- Publishes to NuGet with `dotnet nuget push`

## Development

**Build:**
```bash
dotnet build
```

**Run tests:**
```bash
dotnet test
```

## License

MIT
