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

### Shared.DI.ProvidersConfig.Lite

Simplified provider configuration library with string-based keys and DI integration.

**Installation:**
```bash
dotnet add package Shared.DI.ProvidersConfig.Lite
```

**Key Features:**
- String-based provider keys (no enum required)
- Runtime provider switching via `IProviderSwitcher`
- Default provider support via `_providers.Provider`
- Automatic provider discovery via reflection
- Providers registered in DI container
- Dynamic configuration reload for provider options
- Simplified flat configuration structure
- Lightweight with minimal dependencies

**Usage Example:**

```csharp
// Define provider interface
public interface IMessageProvider
{
    Task SendAsync(string message);
}

// Define holder class
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

// Define provider implementation
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

// Configuration (appsettings.json)
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
    }
  }
}

// DI Registration
services.AddProvidersConfig<IMessageSender, MessageSender, IMessageProvider>(
    configuration,
    ServiceLifetime.Scoped,
    "Providers");

// Usage
var messageSender = serviceProvider.GetRequiredService<IMessageSender>();
await messageSender.SendEmailAsync("Hello!");
await messageSender.SendUsingDefaultAsync("Using default provider!");

// Runtime provider switching
messageSender.SwitchToSms();
await messageSender.SendUsingDefaultAsync("Now using SMS!");
```

**Capabilities:**
- ✅ String-keyed provider selection
- ✅ Runtime provider switching via `IProviderSwitcher`
- ✅ Default provider support via `_providers.Provider`
- ✅ Multiple instances of same provider class with different keys and configurations
- ✅ Automatic provider discovery via reflection
- ✅ DI container registration for all providers with keyed services
- ✅ Options pattern integration
- ✅ Dynamic options reload via `IOptionsSnapshot<T>`
- ✅ Simple flat configuration structure
- ✅ Lightweight dependencies

**Limitations:**
- ❌ No enum-based provider selection
- ❌ Provider type mappings fixed at startup (cannot change which provider type is mapped to a key)
- ❌ No assembly type caching or configurable cache lifetime

**When to Use Lite vs Full:**

Use **Lite** when:
- Simple string-based provider selection is sufficient
- Lighter dependencies are preferred
- Runtime provider switching via `IProviderSwitcher` is sufficient
- Keyed services registration in DI is acceptable

Use **Full** when:
- Enum-based type-safe provider selection is required
- Assembly type caching for performance is important
- More complex provider management scenarios are needed
- Manual provider construction over DI container resolution is preferred

**Important Notes:**
- All provider implementations are registered in DI container at startup
- Provider type mappings (string key → provider type) are fixed at startup
- Configuration changes affect provider options only, not provider type mappings
- Runtime provider switching via `IProviderSwitcher` does not require configuration changes
- Switcher is registered as Singleton, so changes are visible across all scopes
- Use `IOptionsSnapshot<T>` in providers for dynamic configuration reload support
- See `Shared.DI.ProvidersConfig.Lite.Example/EXAMPLES.md` for detailed examples

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
