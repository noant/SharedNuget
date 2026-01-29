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
    private readonly string? _manualVersion;

    public NugetPublisher(string projectName, string apiKey, string source, string? manualVersion = null)
    {
        _projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _manualVersion = manualVersion;
    }

    public async Task PublishAsync()
    {
        Console.WriteLine($"Starting publish process for project: {_projectName}");

        var projectPath = FindProjectPath();
        Console.WriteLine($"Found project at: {projectPath}");

        string newVersion;
        if (!string.IsNullOrWhiteSpace(_manualVersion))
        {
            newVersion = _manualVersion;
            Console.WriteLine($"Using manual version: {newVersion}");
        }
        else
        {
            var latestVersion = await GetLatestVersionAsync();
            newVersion = IncrementVersion(latestVersion);
            Console.WriteLine($"Latest version: {latestVersion?.ToString() ?? "none"}, New version: {newVersion}");
        }

        BuildPackage(projectPath, newVersion, null);
        
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


    private async Task<NuGetVersion?> GetLatestVersionAsync()
    {
        Console.WriteLine($"Querying latest version from {_source}...");
        
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(_source);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        var versions = await resource.GetAllVersionsAsync(
            _projectName,
            cache,
            NullLogger.Instance,
            CancellationToken.None);

        var versionsList = versions?.ToList() ?? new List<NuGetVersion>();
        
        if (versionsList.Count > 0)
        {
            Console.WriteLine($"Found {versionsList.Count} existing versions:");
            foreach (var v in versionsList.OrderByDescending(v => v).Take(5))
            {
                Console.WriteLine($"  - {v}");
            }
        }

        return versionsList
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
        
        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine("Error output:");
            Console.WriteLine(error);
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet pack failed with exit code {process.ExitCode}. See output above for details.");
    }

    private string FindPackagePath(string projectPath, string version)
    {
        var packageFileName = $"{_projectName}.{version}.nupkg";
        
        // Package is created in current directory due to --output .
        var currentDir = Directory.GetCurrentDirectory();
        var packagePath = Path.Combine(currentDir, packageFileName);

        if (File.Exists(packagePath))
            return packagePath;

        // Fallback: check project directory
        var projectDir = Path.GetDirectoryName(projectPath);
        if (projectDir != null)
        {
            packagePath = Path.Combine(projectDir, packageFileName);
            if (File.Exists(packagePath))
                return packagePath;
        }

        throw new FileNotFoundException($"Package file not found. Searched in: {currentDir} and {projectDir}");
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
        
        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine("Error output:");
            Console.WriteLine(error);
        }

        if (process.ExitCode != 0)
        {
            if (error.Contains("409") || error.Contains("already exists"))
            {
                throw new InvalidOperationException($"Package version already exists on NuGet. The version was likely published recently and may take a few minutes to appear in search results.");
            }
            throw new InvalidOperationException($"dotnet nuget push failed with exit code {process.ExitCode}. See output above for details.");
        }
    }
}
