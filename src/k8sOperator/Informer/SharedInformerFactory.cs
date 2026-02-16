using k8s;
using k8s.Models;
using System.Collections.Concurrent;

namespace k8s.Operator.Informer;

public class SharedInformerFactory(IKubernetes kubernetes)
{
    private readonly ConcurrentDictionary<Type, IInternalInformer> _informers = new();

    public IEnumerable<Type> AllTypes() => _informers.Keys;

    public IInformer<TResource> GetInformer<TResource>(string? ns = null, TimeSpan? resyncPeriod = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var type = typeof(TResource);

        var informer = _informers.GetOrAdd(type, _ =>
        {
            var cache = new InMemoryIndexer<TResource>();
            return new ResourceInformer<TResource>(kubernetes, ns, cache, resyncPeriod);
        });

        return (IInformer<TResource>)informer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = _informers.Values.Select(i => i.StartAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task<bool> WaitForCacheSyncAsync(CancellationToken cancellationToken)
    {
        var tasks = _informers.Values.Select(i => i.WaitForSyncAsync(cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.All(r => r);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _informers.Values.Select(i => i.StopAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }
}
