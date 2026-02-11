using k8s.Models;
using k8s.Operator.Builders;
using k8s.Operator.Configuration;
using k8s.Operator.Host;
using k8s.Operator.Host.Commands;
using k8s.Operator.Informer;
using k8s.Operator.Leader;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace k8s.Operator;

/// <summary>
/// Extension methods for registering informer-based controllers.
/// </summary>
public static class OperatorExtensions
{
    extension(CustomResource resource)
    {
        public KubernetesEntityAttribute GetDefinition()
        {
            return resource.GetType().GetCustomAttribute<KubernetesEntityAttribute>()!;
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddOperator(Action<OperatorBuilder>? configure = null)
        {
            var builder = new OperatorBuilder();
            configure?.Invoke(builder);

            services.TryAddSingleton<ControllerDatasource>();
            services.TryAddSingleton<IInformerFactory, InformerFactory>();
            services.TryAddTransient(typeof(IWorkQueue<>), typeof(WorkQueue<>));
            services.TryAddTransient(typeof(IInformer<>), typeof(InformerFactory<>));

            services.TryAddSingleton<IKubernetes>((_) =>
            {
                var config = builder?.Configuration
                    ?? KubernetesClientConfiguration.BuildDefaultConfig();
                return new Kubernetes(config);
            });

            services.TryAddSingleton(sp =>
            {
                var configuration = sp.GetService<IConfiguration>();
                var provider = new OperatorConfigurationProvider(configuration);
                var config = provider.Build();

                // Apply OperatorBuilder overrides if provided
                if (builder.Operator != null)
                {
                    if (!string.IsNullOrEmpty(builder.Operator.Name))
                        config.Name = builder.Operator.Name;
                    if (!string.IsNullOrEmpty(builder.Operator.Namespace))
                        config.Namespace = builder.Operator.Namespace;
                    if (!string.IsNullOrEmpty(builder.Operator.ContainerRegistry))
                        config.ContainerRegistry = builder.Operator.ContainerRegistry;
                    if (!string.IsNullOrEmpty(builder.Operator.ContainerRepository))
                        config.ContainerRepository = builder.Operator.ContainerRepository;
                    if (!string.IsNullOrEmpty(builder.Operator.ContainerTag))
                        config.ContainerTag = builder.Operator.ContainerTag;
                    if (!string.IsNullOrEmpty(builder.Operator.UpdateUrl))
                        config.UpdateUrl = builder.Operator.UpdateUrl;

                }

                config.Validate();

                return config;
            });
            services.TryAddSingleton(sp =>
            {
                var config = sp.GetRequiredService<OperatorConfiguration>();
                var leaderElection = builder.LeaderElection;

                // Set default lease name and namespace if not already set
                if (string.IsNullOrEmpty(leaderElection.LeaseName))
                {
                    leaderElection.LeaseName = $"{config.Name}-leader-election";
                }
                if (string.IsNullOrEmpty(leaderElection.LeaseNamespace))
                {
                    leaderElection.LeaseNamespace = config.Namespace;
                }

                return leaderElection;
            });
            services.TryAddSingleton(sp =>
            {
                var o = sp.GetRequiredService<LeaderElectionOptions>();
                var type = o.ElectionType switch
                {
                    LeaderElectionType.Lease => typeof(LeaderElectionService),
                    LeaderElectionType.Never => typeof(NeverLeaderElectionService),
                    _ => typeof(NoopLeaderElectionService)
                };
                return (ILeaderElectionService)ActivatorUtilities.CreateInstance(sp, type);
            });

            services.TryAddSingleton(x => builder.InstallCommand ?? new InstallCommandOptions());

            // Register command infrastructure
            services.TryAddSingleton(sp =>
            {
                var registry = new CommandRegistry();

                // Discover and register built-in commands
                registry.RegisterCommand<HelpCommand>();
                registry.RegisterCommand<InstallCommand>();
                registry.RegisterCommand<VersionCommand>();
                registry.RegisterCommand<CreateCommand>();
                registry.RegisterCommand<OperatorCommand>();

                // Discover commands in entry assembly
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    registry.DiscoverCommands(entryAssembly);
                }

                return registry;
            });


            services.AddHostedService<OperatorService>();
            return services;
        }
    }

    extension(IHost app)
    {
        public ConventionBuilder<ControllerBuilder> ReconcilerFor<TResource>(Delegate handler)
            where TResource : CustomResource
        {
            var datasource = app.Services.GetRequiredService<ControllerDatasource>();
            return datasource.AddResource<TResource>(handler);
        }

        public async Task<int> RunOperatorAsync()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var registry = app.Services.GetRequiredService<CommandRegistry>();
            var handler = new CommandHandler(app, registry);
            return await handler.HandleAsync(args);
        }

    }
}
