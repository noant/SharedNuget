using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.DI.ProvidersConfig.Lite.Abstractions;

namespace Shared.DI.ProvidersConfig.Lite
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProvidersConfig<THasProvidersInterface, THasProviders, TRealProviderInterface>(
            this IServiceCollection services,
            IConfiguration configuration,
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            string configSection = "Providers")
            where THasProviders : class, THasProvidersInterface, IHasProviders<TRealProviderInterface>
            where TRealProviderInterface : class
        {
            var potentialProviders = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetExportedTypes())
                .Where(x =>
                    !x.IsInterface
                    && !x.IsAbstract
                    && typeof(TRealProviderInterface).IsAssignableFrom(x))
                .Select(x => new
                {
                    ProviderType = x,
                    OptionsType = x
                        .GetInterfaces()
                        .FirstOrDefault(x =>
                            x.IsGenericType
                            && x.GetGenericTypeDefinition() == typeof(IProvider<,>)
                            && x.GetGenericArguments()[0] == typeof(TRealProviderInterface))
                        .GetGenericArguments()[1]
                })
                .ToArray();

            var config = new ProvidersConfigurationRaw();
            configuration
                .GetSection(configSection)
                .Bind(config);

            var configuredProviders = config
                .Select(x => new ProviderHolder<TRealProviderInterface>(
                    x.Key,
                    potentialProviders
                        .Single(z =>
                            z.ProviderType.Name == x.Value.Type
                            || z.ProviderType.FullName == x.Value.Type) is var p ? p.ProviderType : p.ProviderType,
                    p.OptionsType,
                    x.Value.Description
                ))
                .ToArray();

            foreach (var provider in configuredProviders)
            {
                var optionsSnapshotType = typeof(IOptionsSnapshot<>).MakeGenericType(provider.OptionsType);
                var optionsType = typeof(IOptions<>).MakeGenericType(provider.OptionsType);
                var getMethod = optionsSnapshotType.GetMethod("Get");

                var constructors = provider.ProviderType.GetConstructors();
                
                var hasOptionsSnapshot = constructors.Any(c => 
                    c.GetParameters().Any(p => p.ParameterType == optionsSnapshotType));
                var hasOptions = constructors.Any(c => 
                    c.GetParameters().Any(p => p.ParameterType == optionsType));
                var hasRawOptions = constructors.Any(c => 
                    c.GetParameters().Any(p => p.ParameterType == provider.OptionsType));

                if (!hasOptionsSnapshot && !hasOptions && !hasRawOptions)
                {
                    throw new InvalidOperationException(
                        $"Provider type '{provider.ProviderType.FullName}' does not have a constructor " +
                        $"that accepts IOptionsSnapshot<{provider.OptionsType.Name}>, " +
                        $"IOptions<{provider.OptionsType.Name}>, or {provider.OptionsType.Name}.");
                }

                var stubOptionsSnapshotType = hasOptionsSnapshot 
                    ? typeof(StubOptionsSnapshot<>).MakeGenericType(provider.OptionsType) 
                    : null;
                var stubOptionsType = hasOptions 
                    ? typeof(StubOptions<>).MakeGenericType(provider.OptionsType) 
                    : null;

                services.Add(new ServiceDescriptor(
                    serviceType: typeof(TRealProviderInterface),
                    serviceKey: provider.Key,
                    factory: (sp, key) =>
                    {
                        var optionsSnapshot = sp.GetRequiredService(optionsSnapshotType);
                        var options = getMethod.Invoke(optionsSnapshot, [key]);
                        
                        if (hasOptionsSnapshot)
                        {
                            var stubOptionsSnapshot = Activator.CreateInstance(stubOptionsSnapshotType, options, key);
                            return ActivatorUtilities.CreateInstance(sp, provider.ProviderType, stubOptionsSnapshot);
                        }
                        else if (hasOptions)
                        {
                            var stubOptions = Activator.CreateInstance(stubOptionsType, options);
                            return ActivatorUtilities.CreateInstance(sp, provider.ProviderType, stubOptions);
                        }
                        else
                        {
                            return ActivatorUtilities.CreateInstance(sp, provider.ProviderType, options);
                        }
                    },
                    lifetime: lifetime));

                services.Add(new ServiceDescriptor(
                    serviceType: provider.ProviderType,
                    serviceKey: provider.Key,
                    factory: (sp, key) =>
                    {
                        var optionsSnapshot = sp.GetRequiredService(optionsSnapshotType);
                        var options = getMethod.Invoke(optionsSnapshot, [key]);
                        
                        if (hasOptionsSnapshot)
                        {
                            var stubOptionsSnapshot = Activator.CreateInstance(stubOptionsSnapshotType, options, key);
                            return ActivatorUtilities.CreateInstance(sp, provider.ProviderType, stubOptionsSnapshot);
                        }
                        else if (hasOptions)
                        {
                            var stubOptions = Activator.CreateInstance(stubOptionsType, options);
                            return ActivatorUtilities.CreateInstance(sp, provider.ProviderType, stubOptions);
                        }
                        else
                        {
                            return ActivatorUtilities.CreateInstance(sp, provider.ProviderType, options);
                        }
                    },
                    lifetime: lifetime));

                services.Add(new ServiceDescriptor(
                    serviceType: typeof(ProviderHolder<TRealProviderInterface>),
                    factory: provider is var p ? _ => p : null, 
                    lifetime: lifetime));

                var providerConfigSection = configuration
                    .GetSection(configSection)
                    .GetSection(provider.Key)
                    .GetSection("Configuration");

                var optionsConfigureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
                    .GetMethods()
                    .First(m =>
                        m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure) &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 3 &&
                        m.GetParameters()[1].ParameterType == typeof(string) &&
                        m.GetParameters()[2].ParameterType == typeof(IConfiguration));

                var genericOptionsConfigureMethod = optionsConfigureMethod.MakeGenericMethod(provider.OptionsType);
                genericOptionsConfigureMethod.Invoke(null, [services, provider.Key, providerConfigSection]);
            }


            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(ProvidersConfigurationRaw), 
                    factory: _ => config, 
                    lifetime: lifetime));

            services.AddSingleton(
                typeof(IProviderSwitcher<THasProviders, TRealProviderInterface>),
                typeof(LiteProviderSwitcher<THasProviders, TRealProviderInterface>));

            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(IProviders<THasProviders, TRealProviderInterface>),
                    implementationType: typeof(LiteProviders<THasProviders, TRealProviderInterface>), 
                    lifetime: lifetime));

            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(THasProvidersInterface),
                    implementationType: typeof(THasProviders),
                    lifetime: lifetime));

            services.Add(
                new ServiceDescriptor(
                    serviceType: typeof(THasProvidersInterface),
                    implementationType: typeof(THasProviders), 
                    lifetime: lifetime));

            return services;
        }
    }
}