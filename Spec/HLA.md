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
  - `TRealProvider Provider` - first active provider (default)
  - `TRealProvider Of(TEnumProviderType)` - specific provider by enum

**Implementation:**
- `SimpleProviders<TEnumProviderType, TRealProvider>` - provider selector with dynamic filtering
- `SimpleProvidersOptions<TEnumProviderType, TRealProvider>` - options class for active providers configuration
- `ServiceCollectionExtensions` - two registration methods:
  - `AddProvidersConfiguration<THasProviders>()` - register via concrete type
  - `AddProvidersConfiguration<TInterface, TImplementation>()` - register via interface

**Registration Flow:**
1. Application calls `AddProvidersConfiguration<THasProviders>(configuration, lifetime)`
2. Extension reads configuration from `providersConfiguration:{THasProviders.Name}`
3. Discovers **all** provider classes implementing `TRealProvider` via reflection
4. Registers all providers in DI as `TRealProvider` (not just active ones)
5. Configures `IOptions<TOptions>` for each provider from `configurations` section
6. Configures `SimpleProvidersOptions` from `activeProviders` section using `IConfiguration` binding
7. Registers `SimpleProviders` with factory that passes `IServiceProvider` and `IEnumerable<TRealProvider>`

**Resolution Flow:**
1. Application resolves `IProviders<TEnumProviderType, TRealProvider>`
2. `SimpleProviders` constructor receives:
   - `IServiceProvider` for resolving options
   - `IEnumerable<TRealProvider>` - all registered providers
3. On each property/method access:
   - Resolves `IOptionsMonitor<SimpleProvidersOptions>` to get current `ActiveProviders`
   - Filters `_allProviders` by matching provider type names with active provider names
   - Returns filtered provider(s)

**Dynamic Configuration Reload:**
- Uses `IOptionsMonitor<T>` instead of `IOptions<T>` for change tracking
- `Configure(IConfiguration)` creates binding that tracks configuration changes
- When `appsettings.json` changes (with `reloadOnChange: true`):
  - `IOptionsMonitor.CurrentValue` automatically updates
  - Next provider resolution uses new active providers list
  - No application restart needed

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
