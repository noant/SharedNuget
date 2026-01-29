# Feature 001: Providers Configuration Library

IMPORTANT: always use AGENTS.md for rules

## As-Is
No infrastructure for managing multiple provider implementations with runtime/configuration-based selection.

## To-Be
Two projects:
1. **Shared.Utils.NugetPublisher** - console app for automated NuGet package publishing
2. **Shared.DI.ProvidersConfig** - library for DI-based provider configuration and selection

## Projects to Create
- `Shared.Utils.NugetPublisher` - EntryPoint.Console
- `Shared.DI.ProvidersConfig` - library with provider abstractions and DI extensions

## Functionality
Enables configuration-driven provider selection with automatic DI registration, options binding, and version-incremented NuGet publishing.
