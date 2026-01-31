using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Shared.DI.ProvidersConfig;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProvidersConfiguration<THasProviders>(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        string section = "providersConfiguration")
        where THasProviders : class
    {
        return AddProvidersConfigurationCore<THasProviders, THasProviders>(services, configuration, lifetime, section);
    }

    public static IServiceCollection AddProvidersConfiguration<TInterface, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        string section = "providersConfiguration")
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return AddProvidersConfigurationCore<TInterface, TImplementation>(services, configuration, lifetime, section);
    }

    private static IServiceCollection AddProvidersConfigurationCore<TInterface, TImplementation>(
        IServiceCollection services,
        IConfiguration configuration,
        ServiceLifetime lifetime,
        string section)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var hasProvidersType = typeof(TImplementation);
        var hasProvidersInterfaces = hasProvidersType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasProviders<,>))
            .ToList();

        if (hasProvidersInterfaces.Count == 0)
            throw new InvalidOperationException(
                $"Type {hasProvidersType.Name} must implement IHasProviders<TEnumProviderType, TRealProvider>");

        foreach (var hasProvidersInterface in hasProvidersInterfaces)
        {
            var genericArgs = hasProvidersInterface.GetGenericArguments();
            var enumProviderType = genericArgs[0];
            var realProviderType = genericArgs[1];

            RegisterProviders(services, configuration, hasProvidersType, enumProviderType, realProviderType, lifetime, section);
        }

        return services;
    }

    private static void RegisterProviders(
        IServiceCollection services,
        IConfiguration configuration,
        Type hasProvidersType,
        Type enumProviderType,
        Type realProviderType,
        ServiceLifetime lifetime,
        string section = "providersConfiguration")
    {
        var configSectionName = $"{section}:{hasProvidersType.Name}";
        var configSection = configuration.GetSection(configSectionName);
        
        if (!configSection.Exists())
        {
            configSectionName = $"{section}:{hasProvidersType.FullName}";
            configSection = configuration.GetSection(configSectionName);
        }

        if (!configSection.Exists())
            throw new InvalidOperationException(
                $"Configuration section '{configSectionName}' not found");

        var simpleProvidersOptionsType = typeof(SimpleProvidersOptions<,>).MakeGenericType(enumProviderType, realProviderType);

        var optionsConfigureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
            .GetMethods()
            .First(m => 
                m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure) &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType == typeof(IConfiguration));
        
        var genericOptionsConfigureMethod = optionsConfigureMethod.MakeGenericMethod(simpleProvidersOptionsType);
        genericOptionsConfigureMethod.Invoke(null, new object[] { services, configSection });

        var simpleProvidersType = typeof(SimpleProviders<,,>).MakeGenericType(hasProvidersType, enumProviderType, realProviderType);
        var iProvidersType = typeof(IProviders<,>).MakeGenericType(enumProviderType, realProviderType);
        
        var providerSwitcherInterfaceType = typeof(IProviderSwitcher<,,>).MakeGenericType(hasProvidersType, enumProviderType, realProviderType);
        var providerSwitcherImplementationType = typeof(SimpleProviderSwitcher<,,>).MakeGenericType(hasProvidersType, enumProviderType, realProviderType);
        
        services.Add(new ServiceDescriptor(
            providerSwitcherInterfaceType,
            providerSwitcherImplementationType,
            ServiceLifetime.Singleton));

        services.Add(new ServiceDescriptor(
            iProvidersType,
            sp =>
            {
                var providerSwitcher = sp.GetRequiredService(providerSwitcherInterfaceType);
                return Activator.CreateInstance(simpleProvidersType, sp, providerSwitcher, configuration, configSectionName)
                    ?? throw new InvalidOperationException($"Failed to create SimpleProviders");
            },
            lifetime));
    }
}
