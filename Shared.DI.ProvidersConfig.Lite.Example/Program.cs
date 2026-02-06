using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.DI.ProvidersConfig.Lite;
using Shared.DI.ProvidersConfig.Lite.Example.Contracts;
using Shared.DI.ProvidersConfig.Lite.Example.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(configuration);

Console.WriteLine("=== Shared.DI.ProvidersConfig.Lite Example ===\n");

services.AddProvidersConfig<IMessageSender, MessageSender, IMessageProvider>(
    configuration,
    ServiceLifetime.Scoped,
    "Providers");

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
    
    Console.WriteLine("--- Sending Email ---");
    await messageSender.SendEmailAsync("Hello from Email Provider!");
    
    Console.WriteLine("\n--- Sending Primary SMS ---");
    await messageSender.SendSmsAsync("Hello from Primary SMS Provider!");
    
    Console.WriteLine("\n--- Sending Backup SMS (same SmsProvider class, different config) ---");
    await messageSender.SendBackupSmsAsync("Hello from Backup SMS Provider!");
    
    Console.WriteLine("\n--- Sending using Default Provider (configured as 'email') ---");
    await messageSender.SendUsingDefaultAsync("Hello from Default Provider!");
}

Console.WriteLine("\n=== Interactive Demo ===");
Console.WriteLine("1. Press 'e' to switch default provider to Email");
Console.WriteLine("2. Press 's' to switch default provider to Primary SMS");
Console.WriteLine("3. Press 'b' to switch default provider to Backup SMS");
Console.WriteLine("4. Press 'c' to change configuration in appsettings.json and reload");
Console.WriteLine("5. Press any other key to send messages with current settings");
Console.WriteLine("6. Press Ctrl+C to exit\n");

while (true)
{
    var key = Console.ReadKey(true);
    
    using (var scope = serviceProvider.CreateScope())
    {
        var messageSender = scope.ServiceProvider.GetRequiredService<IMessageSender>();
        
        if (key.KeyChar == 'e')
        {
            messageSender.SwitchToEmail();
            Console.WriteLine("\n[Switched default provider to Email]");
        }
        else if (key.KeyChar == 's')
        {
            messageSender.SwitchToSms();
            Console.WriteLine("\n[Switched default provider to Primary SMS]");
        }
        else if (key.KeyChar == 'b')
        {
            messageSender.SwitchToBackupSms();
            Console.WriteLine("\n[Switched default provider to Backup SMS]");
        }
        else if (key.KeyChar == 'c')
        {
            Console.WriteLine("\n[Edit appsettings.json now and press any key when done...]");
            Console.ReadKey(true);
            Console.WriteLine("[Configuration will be reloaded on next provider resolution]");
        }
        
        Console.WriteLine("\n--- Sending with current settings ---");
        await messageSender.SendEmailAsync("Email message");
        await messageSender.SendSmsAsync("Primary SMS message");
        await messageSender.SendBackupSmsAsync("Backup SMS message");
        await messageSender.SendUsingDefaultAsync("Default provider message");
    }
}
