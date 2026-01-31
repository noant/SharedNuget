# Task 003: Default Provider Switcher

## Type
Feature

## Summary
Add default provider configuration and runtime provider switching capability to `Shared.DI.ProvidersConfig`.

## IMPORTANT
Always use AGENTS.md for rules.

## As-Is
- Configuration contains only `activeProviders` dictionary mapping enum keys to provider class names.
- `SimpleProviders.Provider` returns first active provider from filtered list.
- No way to specify default provider in configuration.
- No way to switch active provider at runtime.

## To-Be
- Configuration includes `defaultProvider` field with enum key value.
- New `IProviderSwitcher<THasProviders>` interface for runtime provider switching.
- `SimpleProviders.Provider` uses `IProviderSwitcher` to get current default provider.
- Thread-safe provider switching implementation.

## Components

### 1. Configuration Extension
**Project:** `Shared.DI.ProvidersConfig`

Add `defaultProvider` field to configuration:
- Field type: `string` (enum key)
- Optional field in `SimpleProvidersOptions<TEnumProviderType, TRealProvider>`
- Used as initial value for `IProviderSwitcher`

### 2. IProviderSwitcher Interface
**Project:** `Shared.DI.ProvidersConfig`

```csharp
public interface IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>
    where THasProviders : IHasProviders<TEnumProviderType, TRealProvider>
    where TEnumProviderType : Enum
    where TRealProvider : class
{
    TEnumProviderType Current { get; set; }
}
```

Note: `THasProviders` parameter ensures unique switcher instance per provider group, preventing conflicts when multiple `IHasProviders` share same `TEnumProviderType` and `TRealProvider`.

### 3. SimpleProviderSwitcher Implementation
**Project:** `Shared.DI.ProvidersConfig`

Implementation requirements:
- Thread-safe storage of current provider enum value
- Initial value from `defaultProvider` configuration field
- If `defaultProvider` not set, use first active provider from `activeProviders`
- Throw exception if no active providers available
- Non-nullable `Current` property (get/set)

### 4. SimpleProviders Integration
**Project:** `Shared.DI.ProvidersConfig`

Update `SimpleProviders.Provider`:
- Inject `IProviderSwitcher<IProviders<TEnumProviderType, TRealProvider>, TEnumProviderType, TRealProvider>`
- Use `IProviderSwitcher.Current` to get enum key
- Call `Of(IProviderSwitcher.Current)` to resolve provider

## Configuration Example

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

## Error Handling
- Throw exception if no active providers configured during initialization
- Throw exception if `defaultProvider` value not found in `activeProviders`
- Throw exception if switched provider not found in active providers
