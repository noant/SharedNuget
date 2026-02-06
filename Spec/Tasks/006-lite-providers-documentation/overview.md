# Feature 006: Lite Providers Documentation and Examples

IMPORTANT: always use AGENTS.md for rules

## As-Is
- Shared.DI.ProvidersConfig.Lite exists without documentation
- No README.md in Shared.DI.ProvidersConfig.Lite folder
- No example project demonstrating Lite library usage
- Root README.md doesn't mention Lite version
- No project documentation in spec/projects

## To-Be
- Complete documentation for Shared.DI.ProvidersConfig.Lite
- README.md in Shared.DI.ProvidersConfig.Lite folder explaining features and usage
- Shared.DI.ProvidersConfig.Lite.Examples project demonstrating library usage (based on Shared.DI.ProvidersConfig.Example)
- Root README.md updated with Lite library section
- spec/projects/Shared.DI.ProvidersConfig.Lite.md created

## Projects to Create
- `Shared.DI.ProvidersConfig.Lite.Example` - console application demonstrating Lite library usage

## Documentation to Create
- `Shared.DI.ProvidersConfig.Lite/README.md` - library documentation with features, installation, quick start, configuration schema, capabilities list, limitations list
- `spec/projects/Shared.DI.ProvidersConfig.Lite.md` - technical project specification

## Documentation to Update
- `README.md` (root) - add Shared.DI.ProvidersConfig.Lite section with key differences from full version, capabilities list, limitations list

## Key Differences from Full Version
Shared.DI.ProvidersConfig.Lite is a simplified version:
- Uses string keys instead of enum keys for provider selection
- No assembly type caching or configurable cache lifetime
- Dynamic configuration reload works per provider (IOptions pattern), but no ability to change provider key mapping at runtime
- Simpler configuration structure without enum-keyed hierarchy
- Providers are registered in DI (not constructed manually)
- Lighter dependencies and smaller footprint
- Suitable for scenarios where enum-based selection and runtime provider key switching are not required

## Functionality
Provide comprehensive documentation and working examples for Shared.DI.ProvidersConfig.Lite library to help users understand and integrate the simplified provider configuration system.
