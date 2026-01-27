using k8s.Models;

namespace k8s.Operator.Informer;

public interface IInformerFactory
{
    IInformer<TResource> GetInformer<TResource>(TimeSpan? resyncPeriod = null)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    Task StartAsync(CancellationToken cancellationToken);
    Task<bool> WaitForCacheSyncAsync(CancellationToken cancellationToken);
}
