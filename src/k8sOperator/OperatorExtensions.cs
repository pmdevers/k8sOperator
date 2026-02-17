using k8s.Models;
using k8s.Operator.Cli;
using k8s.Operator.Cli.Commands;
using k8s.Operator.Configuration;
using k8s.Operator.Hosting;
using k8s.Operator.Informer;
using k8s.Operator.Reconciler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace k8s.Operator;

/// <summary>
/// Extension methods for configuring and running the Kubernetes operator.
/// </summary>
public static class OperatorExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the services and configuration required to run a Kubernetes operator using dependency injection.
        /// </summary>
        /// <remarks>This method adds core operator services, including the Kubernetes client, command
        /// registry, informers, reconcilers, and hosted background services. The operator configuration is validated
        /// after applying any custom settings provided through the configure action.</remarks>
        /// <param name="configure">An optional delegate that configures the operator settings. The provided action receives an instance of
        /// OperatorConfiguration to customize before validation.</param>
        /// <returns>The IServiceCollection instance with the operator services registered. This enables method chaining.</returns>
        public IServiceCollection AddOperator(Action<OperatorConfiguration>? configure = null)
        {
            services.TryAddSingleton<IKubernetes>(sp =>
            {
                var operatorConfig = sp.GetService<OperatorConfiguration>();

                var config = operatorConfig?.Kubernetes ??
                    (KubernetesClientConfiguration.IsInCluster()
                    ? KubernetesClientConfiguration.InClusterConfig()
                    : KubernetesClientConfiguration.BuildConfigFromConfigFile());
                return new Kubernetes(config);
            });

            services.TryAddTransient(typeof(IInformer<>), typeof(InformerFactory<>));

            services.AddSingleton(sp =>
            {
                var registry = new CommandRegistry(sp)
                    .Add<HelpCommand>()
                    .Add<OperatorCommand>()
                    .Add<VersionCommand>()
                    .Add<InstallCommand>()
                    .Add<CreateCommand>();
                return registry;
            });

            services.AddSingleton(sp =>
            {
                var configuration = sp.GetService<IConfiguration>();
                var provider = new OperatorConfigurationProvider(configuration);
                var config = provider.Build();
                configure?.Invoke(config);

                config.Validate();

                return config;
            });

            services.AddSingleton<SharedInformerFactory>();
            services.AddSingleton<ReconcilerFactory>();

            services.AddHostedService<OperatorHostedService>();

            return services;
        }
    }

    extension(IHost host)
    {
        /// <summary>
        /// Adds an informer for the specified Kubernetes resource type, enabling resource synchronization and event
        /// handling within the operator.
        /// </summary>
        /// <remarks>Use this method to register an informer for a custom or built-in Kubernetes resource.
        /// Informers provide efficient event-driven updates and periodic resynchronization, allowing the operator to
        /// react to resource changes. The configure action can be used to attach event handlers or customize informer
        /// behavior before it begins watching resources.</remarks>
        /// <typeparam name="TResource">The type of Kubernetes resource to watch.</typeparam>
        /// <param name="ns">The optional namespace to scope the informer. If null, the informer watches resources in all namespaces.</param>
        /// <param name="resyncPeriod">The optional interval at which the informer resynchronizes its state with the Kubernetes API server. If
        /// null, the default resync period is used.</param>
        /// <param name="configure">An optional action to further configure the created informer before it is started.</param>
        public void AddInformer<TResource>(string? ns = null, TimeSpan? resyncPeriod = null, Action<IInformer<TResource>>? configure = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var factory = host.Services.GetRequiredService<SharedInformerFactory>();
            var informer = factory.GetInformer<TResource>(ns, resyncPeriod);
            configure?.Invoke(informer);
        }

        /// <summary>
        /// Adds a reconciler for the specified Kubernetes resource type using the provided reconciliation delegate.
        /// </summary>
        /// <remarks>This method retrieves a ReconcilerFactory from the service provider and creates a
        /// reconciler using the specified delegate. Use this method to register custom reconciliation logic for a
        /// particular resource type within the operator.</remarks>
        /// <typeparam name="TResource">The type of Kubernetes resource that the reconciler will operate on. </typeparam>
        /// <param name="reconcile">A delegate that defines the reconciliation logic to be executed for the specified resource type.</param>
        public void AddReconciler<TResource>(ReconcileDelegate<TResource> reconcile)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var factory2 = host.Services.GetRequiredService<ReconcilerFactory>();
            factory2.Create(reconcile);
        }

        /// <summary>
        /// Executes the root command asynchronously using command-line arguments provided at runtime.
        /// </summary>
        /// <remarks>This method retrieves command-line arguments, initializes the command registry, and
        /// executes the command with the provided arguments. Ensure that the command-line arguments are valid for the
        /// expected command execution.</remarks>
        /// <returns>A task that represents the asynchronous operation of executing the root command.</returns>
        public Task RunOperatorAsync()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var registry = host.Services.GetRequiredService<CommandRegistry>();
            var command = new RootCommand(registry);
            return command.ExecuteAsync(args);
        }
    }
}
