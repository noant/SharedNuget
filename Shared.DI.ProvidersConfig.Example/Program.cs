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

services2.AddProvidersConfiguration<IMessageSender, MessageSender>(configuration);

var serviceProvider2 = services2.BuildServiceProvider();

var switcher = serviceProvider2.GetRequiredService<IProviderSwitcher<MessageSender, MessageType, IMessageProvider>>();

Console.WriteLine("\n=== Interactive Demo: Press 1 for Email, 2 for SMS, any other key to exit ===\n");

while (true)
{
    using (var scope = serviceProvider2.CreateScope())
    {
        var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
        
        Console.WriteLine($"\nCurrent default provider: {switcher.Current}");
        
        await messageSender.SendEmailAsync("Hello from Email Provider!");
        await messageSender.SendSmsAsync("Hello from SMS Provider!");
        await messageSender.SendUsingDefaultAsync("Hello from Default Provider!");
    }
    
    Console.WriteLine("\nPress 1 for Email, 2 for SMS, any other key to exit:");
    var key = Console.ReadKey(true);
    
    if (key.KeyChar == '1')
    {
        switcher.Current = MessageType.Email;
        Console.WriteLine("Switched to Email provider");
    }
    else if (key.KeyChar == '2')
    {
        switcher.Current = MessageType.Sms;
        Console.WriteLine("Switched to SMS provider");
    }
    else
    {
        break;
    }
}

Console.WriteLine("\nAll examples completed successfully!");
