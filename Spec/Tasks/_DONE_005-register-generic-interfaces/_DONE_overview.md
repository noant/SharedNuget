# Task 005: Register Generic Interfaces in DI

## Type
Feature

## Summary
Register additional generic interface types in DI container for `THasProviders`, `IProviderSwitcher`, and `IProviders` to enable resolution through base generic interfaces.

## IMPORTANT
Always use AGENTS.md for rules.

## As-Is
- `THasProviders` registered only as `TInterface` or `TImplementation`
- `SimpleProviderSwitcher` registered only as `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>`
- `SimpleProviders` registered only as `IProviders<TEnumProviderType, TRealProvider>`
- Cannot resolve services through generic base interfaces like `IHasProviders<TEnumProviderType, TRealProvider>`

## To-Be
- `THasProviders` registered as both:
  - `TInterface` (or `TImplementation` if same)
  - `IHasProviders<TEnumProviderType, TRealProvider>`
- `SimpleProviderSwitcher` registered as both:
  - `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>`
  - `IProviderSwitcher<IHasProviders<TEnumProviderType, TRealProvider>, TEnumProviderType, TRealProvider>`
- `SimpleProviders` registered as both:
  - `IProviders<TEnumProviderType, TRealProvider>`
  - Generic resolution through factory

## Components

### 1. THasProviders Registration
**Project:** `Shared.DI.ProvidersConfig`
**File:** `ServiceCollectionExtensions.cs`

Current registration (lines 58-65):
```csharp
if (typeof(TInterface) == typeof(TImplementation))
{
    services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
}
else
{
    services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), lifetime));
}
```

Add registration for `IHasProviders<TEnumProviderType, TRealProvider>`:
- Register `IHasProviders<TEnumProviderType, TRealProvider>` -> `THasProviders`
- Use factory to resolve existing `THasProviders` instance
- Apply same `lifetime` as main registration

### 2. SimpleProviderSwitcher Registration
**Project:** `Shared.DI.ProvidersConfig`
**File:** `ServiceCollectionExtensions.cs`

Current registration (lines 111-114):
```csharp
services.Add(new ServiceDescriptor(
    providerSwitcherInterfaceType,
    providerSwitcherImplementationType,
    ServiceLifetime.Singleton));
```

Add registration for generic base interface:
- Register `IProviderSwitcher<IHasProviders<TEnumProviderType, TRealProvider>, TEnumProviderType, TRealProvider>`
- Use factory to resolve existing `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>` instance
- Keep `ServiceLifetime.Singleton`

### 3. SimpleProviders Registration
**Project:** `Shared.DI.ProvidersConfig`
**File:** `ServiceCollectionExtensions.cs`

Current registration (lines 116-124):
```csharp
services.Add(new ServiceDescriptor(
    iProvidersType,
    sp =>
    {
        var providerSwitcher = sp.GetRequiredService(providerSwitcherInterfaceType);
        return Activator.CreateInstance(simpleProvidersType, sp, providerSwitcher, configuration, configSectionName)
            ?? throw new InvalidOperationException($"Failed to create SimpleProviders");
    },
    lifetime));
```

Already registered as `IProviders<TEnumProviderType, TRealProvider>`, no additional registration needed.

## Implementation Details

### Registration Order
1. Register `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>` (existing)
2. Register `IProviderSwitcher<IHasProviders<TEnumProviderType, TRealProvider>, TEnumProviderType, TRealProvider>` (new)
3. Register `IProviders<TEnumProviderType, TRealProvider>` (existing)
4. Register `THasProviders` as `TInterface`/`TImplementation` (existing)
5. Register `IHasProviders<TEnumProviderType, TRealProvider>` -> `THasProviders` (new)

### Factory Resolution Pattern
For generic interface registrations, use factory pattern:
```csharp
services.Add(new ServiceDescriptor(
    genericInterfaceType,
    sp => sp.GetRequiredService(concreteInterfaceType),
    lifetime));
```

## Usage Example

After implementation, all these resolutions will work:

```csharp
// Existing - works now
var messageSender1 = serviceProvider.GetRequiredService<MessageSender>();
var switcher1 = serviceProvider.GetRequiredService<IProviderSwitcher<MessageSender, ProviderType, IMessageProvider>>();
var providers1 = serviceProvider.GetRequiredService<IProviders<ProviderType, IMessageProvider>>();

// New - will work after implementation
var messageSender2 = serviceProvider.GetRequiredService<IHasProviders<ProviderType, IMessageProvider>>();
var switcher2 = serviceProvider.GetRequiredService<IProviderSwitcher<IHasProviders<ProviderType, IMessageProvider>, ProviderType, IMessageProvider>>();
```

## Error Handling
- No additional error handling needed
- Factory resolution will throw if underlying service not registered
- Existing validation remains unchanged
