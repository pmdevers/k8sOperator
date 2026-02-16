using k8s.Models;
using k8s.Operator.Configuration;
using k8s.Operator.Informer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace k8s.Operator.Reconciler;

public class ReconcileContext<T>(
    IServiceProvider serviceProvider,
    IInformer<T> informer,
    IWorkQueue<T> queue,
    T resource,
    CancellationToken cancellationToken)
    where T : IKubernetesObject<V1ObjectMeta>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public T Resource { get; } = resource;
    public IInformer<T> Informer => informer;
    public IWorkQueue<T> Queue => queue;
    public ILogger<T> Logger => ServiceProvider.GetRequiredService<ILogger<T>>();
    public IKubernetes Kubernetes => ServiceProvider.GetRequiredService<IKubernetes>();
    public OperatorConfiguration Configuration => ServiceProvider.GetRequiredService<OperatorConfiguration>();
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public IInformer<TResource> GetInformer<TResource>(string? ns = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var factory = ServiceProvider.GetRequiredService<SharedInformerFactory>();
        return factory.GetInformer<TResource>(ns);
    }
}
