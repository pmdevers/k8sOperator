using k8s.Operator;
using k8s.Operator.Cache;
using k8s.Operator.Configuration;
using k8s.Operator.Models;
using System.Collections.Concurrent;

namespace k8s.Operator.Informer;

public class InformerFactory(IKubernetes client, OperatorConfiguration configuration) : IInformerFactory
{
    private readonly IKubernetes _client = client;
    private readonly OperatorConfiguration _configuration = configuration;
    private readonly ConcurrentDictionary<Type, IInformerInternal> _informers = new();

    public IInformer<TResource> GetInformer<TResource>(TimeSpan? resyncPeriod = null)
        where TResource : CustomResource
    {
        var type = typeof(IInformer<TResource>);

        var informer = _informers.GetOrAdd(type, _ =>
        {
            var cache = new ResourceCache<TResource>();

            var ns = _configuration.Namespace != "all" ? _configuration.Namespace
                : null;

            return new ResourceInformer<TResource>(_client, ns, cache, resyncPeriod);
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


public class InformerFactory<TResource>(IInformerFactory factory) : IInformer<TResource>
    where TResource : CustomResource
{
    private readonly IInformer<TResource> _informer = factory.GetInformer<TResource>();

    public IAsyncEnumerable<WatchEvent<TResource>> Events => _informer.Events;

    public bool HasSynced => _informer.HasSynced;

    public TResource? Get(string name, string? ns = null)
        => _informer.Get(name, ns);

    public IReadOnlyList<TResource> List()
        => _informer.List();
}
