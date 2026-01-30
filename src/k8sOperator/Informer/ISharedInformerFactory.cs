using k8s.Operator.Models;

namespace k8s.Operator.Informer;

public interface IInformerFactory
{
    IInformer<TResource> GetInformer<TResource>(TimeSpan? resyncPeriod = null)
        where TResource : CustomResource;

    Task StartAsync(CancellationToken cancellationToken);
    Task<bool> WaitForCacheSyncAsync(CancellationToken cancellationToken);
}
