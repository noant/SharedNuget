# Shared.Utils.NugetPublisher

IMPORTANT: always use AGENTS.md for rules

## Purpose
Console application for automated NuGet package publishing with version increment.

## Dependencies
- System.CommandLine
- NuGet.Protocol
- NuGet.Versioning

## Components

### Program.cs
Entry point with command-line argument parsing using System.CommandLine.

### NugetPublisher.cs
Main logic for package publishing:
- `FindProjectPath()` - locates .csproj file
- `GetLatestVersionAsync()` - queries NuGet.org for latest version
- `IncrementVersion()` - increments patch version (1.0.x)
- `BuildPackage()` - executes `dotnet pack`
- `PushPackage()` - executes `dotnet nuget push`

## Usage
```bash
dotnet run --project Shared.Utils.NugetPublisher -- --project Shared.DI.ProvidersConfig --api-key YOUR_API_KEY

# With custom source
dotnet run --project Shared.Utils.NugetPublisher -- -p Shared.DI.ProvidersConfig -k YOUR_API_KEY -s https://custom.nuget.org/v3/index.json
```

## Version Strategy
Simple 1.0.x increment where x is patch number. Always increments from latest non-prerelease version on NuGet.org.
