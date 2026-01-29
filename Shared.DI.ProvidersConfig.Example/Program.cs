using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.DI.ProvidersConfig;
using Shared.DI.ProvidersConfig.Example.Contracts;
using Shared.DI.ProvidersConfig.Example.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

Console.WriteLine("=== Example 1: Registration via concrete type ===\n");

services.AddProvidersConfiguration<MessageSender>(configuration, ServiceLifetime.Scoped);
services.AddScoped<MessageSender>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var messageSender = scope.ServiceProvider.GetRequiredService<MessageSender>();
    
    await messageSender.SendEmailAsync("Hello from Email Provider!");
    await messageSender.SendSmsAsync("Hello from SMS Provider!");
    await messageSender.SendUsingDefaultAsync("Hello from Default Provider!");
}

Console.WriteLine("\n=== Example 2: Registration via interface ===\n");

var services2 = new ServiceCollection();
services2.AddSingleton<IConfiguration>(configuration);

services2.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration, ServiceLifetime.Scoped);
services2.AddScoped<IMessageSender, MessageSender>();

var serviceProvider2 = services2.BuildServiceProvider();

while (true)
{
    using (var scope = serviceProvider2.CreateScope())
    {
        var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
        
        await messageSender.SendEmailAsync("Hello from Email Provider!");
        await messageSender.SendSmsAsync("Hello from SMS Provider!");
        await messageSender.SendUsingDefaultAsync("Hello from Default Provider!");
    }
    await Task.Delay(5000);
}

Console.WriteLine("\nAll examples completed successfully!");
