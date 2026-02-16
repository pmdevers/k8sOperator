using k8s;
using k8s.Models;

namespace k8s.Operator.Informer;

public interface IInformer<T>
    where T : IKubernetesObject<V1ObjectMeta>
{
    event Action<T, CancellationToken> OnAdd;
    event Action<T?, T, CancellationToken> OnUpdate;
    event Action<T, CancellationToken> OnDelete;

    IIndexer<T> Indexer { get; }
    IEnumerable<T> List();
}

public interface IInternalInformer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task<bool> WaitForSyncAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public record WatchEvent<TResource>
{
    public WatchEventType Type { get; init; }
    public required TResource Object { get; init; }
}
