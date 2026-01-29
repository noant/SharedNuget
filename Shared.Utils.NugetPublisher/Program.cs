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

var descriptionOption = new Option<string?>(
    aliases: new[] { "--description", "-d" },
    description: "Package description",
    getDefaultValue: () => null);

var rootCommand = new RootCommand("NuGet Publisher - builds and publishes NuGet packages with auto-incremented versions");
rootCommand.AddOption(projectNameOption);
rootCommand.AddOption(apiKeyOption);
rootCommand.AddOption(sourceOption);
rootCommand.AddOption(descriptionOption);

rootCommand.SetHandler(async (projectName, apiKey, source, description) =>
{
    var publisher = new NugetPublisher(projectName, apiKey, source, description);
    await publisher.PublishAsync();
}, projectNameOption, apiKeyOption, sourceOption, descriptionOption);

return await rootCommand.InvokeAsync(args);
