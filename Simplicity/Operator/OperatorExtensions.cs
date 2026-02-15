using k8s;
using k8s.Models;
using Simplicity.Operator.Cli;
using Simplicity.Operator.Cli.Commands;
using Simplicity.Operator.Configuration;
using Simplicity.Operator.Hosting;
using Simplicity.Operator.Informer;
using Simplicity.Operator.Reconciler;

namespace Simplicity.Operator;

public static class OperatorExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpperator(Action<OperatorConfiguration>? configure = null)
        {
            services.AddSingleton<IKubernetes>(sp =>
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                return new Kubernetes(config);
            });

            services.AddSingleton(sp =>
            {
                var registry = new CommandRegistry(sp);
                registry.RegisterCommand(typeof(HelpCommand));
                registry.RegisterCommand(typeof(OperatorCommand));
                registry.RegisterCommand(typeof(VersionCommand));
                registry.RegisterCommand(typeof(InstallCommand));
                return registry;
            });

            services.AddSingleton(sp =>
            {
                var configuration = sp.GetService<IConfiguration>();
                var provider = new OperatorConfigurationProvider(configuration);
                var config = provider.Build();
                configure?.Invoke(config);
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
        public void AddInformer<TResource>(Action<IInformer<TResource>>? configure = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var factory = host.Services.GetRequiredService<SharedInformerFactory>();
            var informer = factory.GetInformer<TResource>();
            configure?.Invoke(informer);
        }

        public void AddReconciler<TResource>(ReconcileDelegate<TResource> reconcile)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var factory2 = host.Services.GetRequiredService<ReconcilerFactory>();
            var reconciler = factory2.Create(reconcile);
        }

        public Task RunOperatorAsync()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var registry = host.Services.GetRequiredService<CommandRegistry>();
            var command = new RootCommand(host, registry);
            return command.ExecuteAsync(args);
        }
    }
}
