# 001: NuGet Publisher Console App

IMPORTANT: always use AGENTS.md for rules

## As-Is
Manual NuGet package building and publishing process.

## To-Be
Automated console app `Shared.Utils.NugetPublisher` that:
- Accepts project name and NuGet API key via System.CommandLine
- Queries NuGet.org for latest version of package
- Increments patch version (1.0.x pattern)
- Builds and publishes package with new version

## Implementation
- Project: `Shared.Utils.NugetPublisher`
- Type: Console application
- Dependencies: System.CommandLine, NuGet.Protocol
- Commands: `dotnet pack`, `dotnet nuget push`
- Version strategy: Simple 1.0.x increment (x++)
