# High-Level Architecture

IMPORTANT: always use AGENTS.md for rules

## Overview
SharedNuget is a collection of reusable .NET libraries published as NuGet packages.

## Projects

### Shared.DI.ProvidersConfig
Library providing configuration-driven provider selection with DI integration.

**Abstractions:**
- `IHasProviders<TEnumProviderType, TRealProvider>` - marker interface for classes that use providers
- `IProvider<TRealProvider, TOptions>` - interface for provider implementations with options
- `IProviders<TEnumProviderType, TRealProvider>` - provider selector interface with:
  - `IReadOnlyList<TRealProvider> Providers` - all active providers
  - `TRealProvider Provider` - default provider (uses IProviderSwitcher)
  - `TRealProvider Of(TEnumProviderType)` - specific provider by enum
- `IProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>` - runtime provider switching with:
  - `TEnumProviderType Current { get; set; }` - current default provider (thread-safe)

**Implementation:**
- `SimpleProviders<TEnumProviderType, TRealProvider>` - provider selector with dynamic filtering
- `SimpleProvidersOptions<TEnumProviderType, TRealProvider>` - options class with:
  - `DefaultProvider` - optional default provider enum key
  - `ActiveProviders` - dictionary mapping enum keys to provider class names
  - `Configurations` - provider-specific configuration objects
- `SimpleProviderSwitcher<THasProviders, TEnumProviderType, TRealProvider>` - thread-safe provider switching with static state
- `ServiceCollectionExtensions` - two registration methods:
  - `AddProvidersConfiguration<THasProviders>()` - register via concrete type
  - `AddProvidersConfiguration<TInterface, TImplementation>()` - register via interface

**Registration Flow:**
1. Application calls `AddProvidersConfiguration<THasProviders>(configuration, lifetime)`
2. Extension reads configuration from `providersConfiguration:{THasProviders.Name}`
3. Discovers **all** provider classes implementing `TRealProvider` via reflection
4. Registers all providers in DI as `TRealProvider` (not just active ones)
5. Configures `IOptions<TOptions>` for each provider from `configurations` section
6. Configures `SimpleProvidersOptions` from configuration (including `defaultProvider` and `activeProviders`)
7. Registers `IProviderSwitcher` as Singleton (persists state across lifetimes)
8. Registers `SimpleProviders` with factory that passes `IServiceProvider`, `IEnumerable<TRealProvider>`, and `IProviderSwitcher`

**Resolution Flow:**
1. Application resolves `IProviders<TEnumProviderType, TRealProvider>`
2. `SimpleProviders` constructor receives:
   - `IServiceProvider` for resolving options
   - `IEnumerable<TRealProvider>` - all registered providers
   - `IProviderSwitcher` - for getting current default provider
3. On `Provider` property access:
   - Gets current enum value from `IProviderSwitcher.Current`
   - Calls `Of(current)` to resolve provider
4. On `Of(TEnumProviderType)` method call:
   - Resolves `IOptionsMonitor<SimpleProvidersOptions>` to get current `ActiveProviders`
   - Filters `_allProviders` by matching provider type names with active provider names
   - Returns specific provider by enum key
5. On `Providers` property access:
   - Returns all active providers as read-only list

**Dynamic Configuration Reload:**
- Uses `IOptionsMonitor<T>` instead of `IOptions<T>` for change tracking
- `Configure(IConfiguration)` creates binding that tracks configuration changes
- When `appsettings.json` changes (with `reloadOnChange: true`):
  - `IOptionsMonitor.CurrentValue` automatically updates
  - Next provider resolution uses new active providers list
  - No application restart needed

**Default Provider & Runtime Switching:**
- `defaultProvider` field in configuration specifies initial default provider
- If not specified, first active provider is used
- `IProviderSwitcher.Current` allows runtime switching without configuration changes
- Switcher state is static and persists across all instances with same generic parameters
- Switcher registered as Singleton to maintain state across DI lifetimes
- Thread-safe implementation using lock

### Shared.Utils.NugetPublisher
Console application for automated NuGet package publishing with version increment.

**EntryPoint:** Console app with System.CommandLine
**Arguments:**
- `--project` / `-p` - project name to publish
- `--api-key` / `-k` - NuGet API key
- `--source` / `-s` - NuGet source URL (default: nuget.org)

**Flow:**
1. Find project .csproj file
2. Query NuGet.org for latest version using NuGet.Protocol
3. Increment patch version (1.0.x pattern)
4. Execute `dotnet pack` with new version
5. Execute `dotnet nuget push` with API key
