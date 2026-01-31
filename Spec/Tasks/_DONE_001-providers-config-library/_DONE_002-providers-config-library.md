# 002: Providers Configuration Library

IMPORTANT: always use AGENTS.md for rules

## As-Is
No abstraction for managing multiple provider implementations with configuration-based selection.

## To-Be
Library `Shared.DI.ProvidersConfig` with:

### Core Interfaces
- `IHasProviders<TEnumProviderType, TRealProvider>` - holder class marker
- `IProvider<TRealProvider, TOptions>` - provider interface with options
- `IProviders<TEnumProviderType, TRealProvider>` - provider selector with:
  - `Of(TEnumProviderType)` - runtime selection
  - `Provider` - single configured provider
- `ProviderHolder<TEnumProviderType, TProvider>` - internal storage

### Implementation
- `SimpleProviders<TEnumProviderType, TRealProvider>` - main implementation

### Extension Method
`AddProvidersConfiguration<THasProviders>(IServiceCollection, ServiceLifetime)`

Configuration structure:
```json
{
  "providersConfiguration": {
    "{THasProviders.Name}": {
      "activeProviders": {
        "EnumValue": "ProviderClassName"
      },
      "configurations": {
        "ProviderClassName": { /* TOptions */ }
      }
    }
  }
}
```

Behavior:
1. Resolve configuration section from `providersConfiguration->{THasProviders.Name}` (fallback to FullName)
2. Discover all provider classes via reflection from activeProviders
3. Register providers in ProviderHolder with specified lifetime
4. Register each provider class both as itself and as TRealProvider interface
5. Configure IOptions<TOptions> for each provider from configurations section

## Key Features
- Automatic provider registration - no need to manually register providers in DI
- Automatic IOptions configuration from JSON configurations section
- Support for runtime provider selection via `IProviders<>.Of(enumValue)`
- Support for direct single provider injection via `INotificationProvider` or concrete type
- Provider classes can be resolved directly from DI container

## Usage Patterns

### Pattern 1: Multiple Providers with Runtime Selection
Holder class injects `IProviders<TEnum, TInterface>` and selects provider at runtime.

### Pattern 2: Single Provider Direct Injection
Holder class injects `TInterface` directly, gets first configured provider automatically.

### Pattern 3: Direct Provider Access
Concrete provider class (e.g., `TelegramProvider`) can be resolved directly from DI.
