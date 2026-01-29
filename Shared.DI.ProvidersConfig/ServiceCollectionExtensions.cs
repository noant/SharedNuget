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
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        string section = "providersConfiguration")
        where THasProviders : class
    {
        return AddProvidersConfigurationCore<THasProviders, THasProviders>(services, configuration, lifetime, section);
    }

    public static IServiceCollection AddProvidersConfiguration<TInterface, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
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

        var activeProvidersSection = configSection.GetSection("activeProviders");
        var configurationsSection = configSection.GetSection("configurations");

        var allLoadedTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a => a.GetExportedTypes())
            .ToList();

        var allProviderTypes = allLoadedTypes
            .Where(t => 
                t.IsClass && 
                !t.IsAbstract && 
                realProviderType.IsAssignableFrom(t))
            .ToList();

        foreach (var providerType in allProviderTypes)
        {
            services.Add(new ServiceDescriptor(providerType, providerType, lifetime));
            services.Add(new ServiceDescriptor(realProviderType, providerType, lifetime));

            var providerInterfaces = providerType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProvider<,>))
                .ToList();

            foreach (var providerInterface in providerInterfaces)
            {
                var optionsType = providerInterface.GetGenericArguments()[1];
                var providerConfigSection = configurationsSection.GetSection(providerType.Name);

                if (providerConfigSection.Exists())
                {
                    var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
                        .GetMethods()
                        .First(m => 
                            m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure) &&
                            m.GetGenericArguments().Length == 1 &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[1].ParameterType == typeof(IConfiguration));

                    var genericConfigureMethod = configureMethod.MakeGenericMethod(optionsType);
                    genericConfigureMethod.Invoke(null, new object[] { services, providerConfigSection });
                }
            }
        }

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

        var simpleProvidersType = typeof(SimpleProviders<,>).MakeGenericType(enumProviderType, realProviderType);
        var iProvidersType = typeof(IProviders<,>).MakeGenericType(enumProviderType, realProviderType);

        services.Add(new ServiceDescriptor(
            iProvidersType,
            sp =>
            {
                var allProviders = sp.GetServices(realProviderType);
                return Activator.CreateInstance(simpleProvidersType, sp, allProviders)
                    ?? throw new InvalidOperationException($"Failed to create SimpleProviders");
            },
            lifetime));
    }
}
