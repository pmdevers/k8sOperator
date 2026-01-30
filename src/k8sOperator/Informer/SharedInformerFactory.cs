using k8s.Operator;
using k8s.Operator.Cache;
using k8s.Operator.Models;
using System.Collections.Concurrent;

namespace k8s.Operator.Informer;

public class InformerFactory(IKubernetes client) : IInformerFactory
{
    private readonly IKubernetes _client = client;
    private readonly ConcurrentDictionary<Type, IInformerInternal> _informers = new();

    public IInformer<TResource> GetInformer<TResource>(TimeSpan? resyncPeriod = null)
        where TResource : CustomResource
    {
        var type = typeof(IInformer<TResource>);

        var informer = _informers.GetOrAdd(type, _ =>
        {
            var cache = new ResourceCache<TResource>();
            return new ResourceInformer<TResource>(_client, cache, resyncPeriod);
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
}
