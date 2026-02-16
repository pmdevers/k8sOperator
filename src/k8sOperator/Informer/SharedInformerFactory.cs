using k8s.Models;
using k8s.Operator.Configuration;
using System.Collections.Concurrent;

namespace k8s.Operator.Informer;

public class InformerFactory<T>(SharedInformerFactory factory, OperatorConfiguration config) : IInformer<T>
    where T : IKubernetesObject<V1ObjectMeta>
{
    private IInformer<T> _informer = factory.GetInformer<T>(config.Namespace);
    public IIndexer<T> Indexer => _informer.Indexer;

    public event Action<T, CancellationToken> OnAdd
    {
        add
        {
            _informer.OnAdd += value;
        }

        remove
        {
            _informer.OnAdd -= value;
        }
    }

    public event Action<T?, T, CancellationToken> OnUpdate
    {
        add
        {
            _informer.OnUpdate += value;
        }

        remove
        {
            _informer.OnUpdate -= value;
        }
    }

    public event Action<T, CancellationToken> OnDelete
    {
        add
        {
            _informer.OnDelete += value;
        }

        remove
        {
            _informer.OnDelete -= value;
        }
    }

    public IEnumerable<T> List()
    {
        return _informer.List();
    }
}

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
