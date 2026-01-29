using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Shared.Utils.NugetPublisher;

public class NugetPublisher
{
    private readonly string _projectName;
    private readonly string _apiKey;
    private readonly string _source;
    private readonly string? _description;

    public NugetPublisher(string projectName, string apiKey, string source, string? description = null)
    {
        _projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _description = description;
    }

    public async Task PublishAsync()
    {
        Console.WriteLine($"Starting publish process for project: {_projectName}");

        var projectPath = FindProjectPath();
        Console.WriteLine($"Found project at: {projectPath}");

        var description = GetDescription(projectPath);
        if (!string.IsNullOrWhiteSpace(description))
        {
            Console.WriteLine($"Using description: {description}");
        }

        var latestVersion = await GetLatestVersionAsync();
        var newVersion = IncrementVersion(latestVersion);
        Console.WriteLine($"Latest version: {latestVersion?.ToString() ?? "none"}, New version: {newVersion}");

        BuildPackage(projectPath, newVersion, description);
        
        var packagePath = FindPackagePath(projectPath, newVersion);
        Console.WriteLine($"Package created at: {packagePath}");

        PushPackage(packagePath);
        
        Console.WriteLine($"Successfully published {_projectName} version {newVersion}");
    }

    private string FindProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var projectFile = $"{_projectName}.csproj";
        
        var searchPaths = new[]
        {
            Path.Combine(currentDir, _projectName, projectFile),
            Path.Combine(currentDir, projectFile),
            Path.Combine(Directory.GetParent(currentDir)?.FullName ?? currentDir, _projectName, projectFile)
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException($"Project file {projectFile} not found in expected locations");
    }

    private string? GetDescription(string projectPath)
    {
        if (!string.IsNullOrWhiteSpace(_description))
        {
            return _description;
        }

        var projectDir = Path.GetDirectoryName(projectPath);
        if (projectDir == null)
            return null;

        var descriptionFile = Path.Combine(projectDir, "nuget_description.txt");
        
        if (File.Exists(descriptionFile))
        {
            var content = File.ReadAllText(descriptionFile).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }

        return null;
    }

    private async Task<NuGetVersion?> GetLatestVersionAsync()
    {
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(_source);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        var versions = await resource.GetAllVersionsAsync(
            _projectName,
            cache,
            NullLogger.Instance,
            CancellationToken.None);

        return versions
            .Where(v => !v.IsPrerelease)
            .OrderByDescending(v => v)
            .FirstOrDefault();
    }

    private static string IncrementVersion(NuGetVersion? currentVersion)
    {
        if (currentVersion == null)
            return "1.0.0";

        var major = currentVersion.Major;
        var minor = currentVersion.Minor;
        var patch = currentVersion.Patch + 1;

        return $"{major}.{minor}.{patch}";
    }

    private void BuildPackage(string projectPath, string version, string? description)
    {
        Console.WriteLine($"Building package with version {version}...");

        var arguments = $"pack \"{projectPath}\" -c Release /p:Version={version} --output .";
        
        if (!string.IsNullOrWhiteSpace(description))
        {
            arguments += $" /p:Description=\"{description}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start dotnet pack process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine(output);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet pack failed with exit code {process.ExitCode}: {error}");
    }

    private string FindPackagePath(string projectPath, string version)
    {
        var projectDir = Path.GetDirectoryName(projectPath) 
            ?? throw new InvalidOperationException("Could not determine project directory");
        
        var packageFileName = $"{_projectName}.{version}.nupkg";
        var packagePath = Path.Combine(projectDir, packageFileName);

        if (!File.Exists(packagePath))
            throw new FileNotFoundException($"Package file not found: {packagePath}");

        return packagePath;
    }

    private void PushPackage(string packagePath)
    {
        Console.WriteLine($"Pushing package to {_source}...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"nuget push \"{packagePath}\" --api-key {_apiKey} --source {_source}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start dotnet nuget push process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine(output);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet nuget push failed with exit code {process.ExitCode}: {error}");
    }
}
