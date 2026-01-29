using System.CommandLine;
using Shared.Utils.NugetPublisher;

var projectNameOption = new Option<string>(
    aliases: new[] { "--project", "-p" },
    description: "Project name to build and publish")
{
    IsRequired = true
};

var apiKeyOption = new Option<string>(
    aliases: new[] { "--api-key", "-k" },
    description: "NuGet API key for publishing")
{
    IsRequired = true
};

var sourceOption = new Option<string>(
    aliases: new[] { "--source", "-s" },
    description: "NuGet source URL",
    getDefaultValue: () => "https://api.nuget.org/v3/index.json");

var versionOption = new Option<string?>(
    aliases: new[] { "--package-version", "-v" },
    description: "Package version (if not specified, auto-increments from latest)",
    getDefaultValue: () => null);

var rootCommand = new RootCommand("NuGet Publisher - builds and publishes NuGet packages with auto-incremented versions");
rootCommand.AddOption(projectNameOption);
rootCommand.AddOption(apiKeyOption);
rootCommand.AddOption(sourceOption);
rootCommand.AddOption(versionOption);

rootCommand.SetHandler(async (projectName, apiKey, source, version) =>
{
    var publisher = new NugetPublisher(projectName, apiKey, source, version);
    await publisher.PublishAsync();
}, projectNameOption, apiKeyOption, sourceOption, versionOption);

return await rootCommand.InvokeAsync(args);
