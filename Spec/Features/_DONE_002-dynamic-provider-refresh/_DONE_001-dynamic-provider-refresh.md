# 001: Dynamic Provider Refresh on Configuration Change

IMPORTANT: always use AGENTS.md for rules

## As-Is
- Providers are resolved once during service registration
- Active providers list is cached statically during registration
- Configuration changes require application restart
- `SimpleProviders.Providers` field is private, preventing access to active providers list

## To-Be
Dynamic provider resolution with fresh configuration on each resolve:

### Registration Strategy
Three-level descriptor registration in `AddProvidersConfiguration`:

1. **Register SimpleProviders** - returns fresh active providers on each resolve
   - Descriptor resolves current configuration state
   - Determines active providers dynamically per resolve
   - Acts as holder for current provider set

2. **Register IEnumerable<TRealProvider>** - returns all active provider instances
   - Descriptor resolves `SimpleProviders<TEnumProviderType, TRealProvider>`
   - Accesses public immutable `Providers` property
   - Returns collection of all currently active providers

3. **Register single TRealProvider** - returns default provider
   - Descriptor resolves `IEnumerable<TRealProvider>` from previous registration
   - Returns `FirstOrDefault()`
   - Throws exception if null

### Implementation Details

#### SimpleProviders Changes
- Add constructor parameter `IServiceProvider` for dynamic resolution
- Add internal `Options` class with `Dictionary<TEnumProviderType, string> ActiveProviders`
- Configure `SimpleProvidersOptions<TEnumProviderType, TRealProvider>` from configuration's `activeProviders` section
- On each property/method access:
  - Resolve fresh `IOptions<SimpleProvidersOptions<TEnumProviderType, TRealProvider>>` from `IServiceProvider`
  - Get current `ActiveProviders` dictionary from options
  - Resolve provider instances from DI based on current active providers
  - Return fresh provider list/instance

#### Properties and Methods Behavior
- `Providers` property:
  - Resolves options on each access
  - Returns `IReadOnlyList<TRealProvider>` of currently active providers
  - Dynamically builds list from current configuration
  
- `Provider` property:
  - Resolves options on each access
  - Returns first active provider from current configuration
  - Throws if no providers configured

- `Of(TEnumProviderType)` method:
  - Resolves options on each access
  - Looks up provider name in current `ActiveProviders` dictionary
  - Resolves and returns provider instance from DI
  - Throws if enum value not in current active providers

#### Descriptor Logic
- Register `SimpleProvidersOptions<TEnumProviderType, TRealProvider>` with IOptions
- Configure options from `activeProviders` configuration section (Dictionary<enum, className>)
- Register `SimpleProviders<TEnumProviderType, TRealProvider>` with factory delegate
- Cache static metadata (types, reflection info) outside descriptor
- Descriptor resolves `IServiceProvider` and passes to `SimpleProviders` constructor

### Key Benefits
- Configuration changes reflected without restart
- Fresh provider set on each resolution
- Maintains performance through natural caching
- Supports dynamic provider switching

## Technical Approach
- Use `ServiceDescriptor` factory delegates for dynamic resolution
- Keep type metadata and configuration sections cached
- Resolve providers through `IServiceProvider` in descriptor
- Ensure immutability of exposed collections
