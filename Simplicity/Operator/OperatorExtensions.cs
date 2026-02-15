using k8s;
using k8s.Models;
using Simplicity.Operator.Hosting;
using Simplicity.Operator.Informer;
using Simplicity.Operator.Reconciler;

namespace Simplicity.Operator;

public static class OperatorExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpperator()
        {
            services.AddSingleton<IKubernetes>(sp =>
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                return new Kubernetes(config);
            });

            services.AddSingleton<SharedInformerFactory>();
            services.AddSingleton<ReconcilerFactory>();

            services.AddHostedService<LeaderElectionHostedService>();

            return services;
        }
    }

    extension(IHost host)
    {
        public void AddInformer<TResource>(Action<IInformer<TResource>> configure)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var factory = host.Services.GetRequiredService<SharedInformerFactory>();
            var informer = factory.GetInformer<TResource>();
            configure(informer);
        }

        public void AddReconciler<TResource>(ReconcileDelegate<TResource> reconcile)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var factory2 = host.Services.GetRequiredService<ReconcilerFactory>();
            var reconciler = factory2.Create(reconcile);
        }
    }
}
