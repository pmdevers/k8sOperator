using k8s;
using k8s.Models;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Simplicity.Operator.Informer;

public interface IInformer<T>
    where T : IKubernetesObject<V1ObjectMeta>
{
    event Action<T, CancellationToken> OnAdd;
    event Action<T, T, CancellationToken> OnUpdate;
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
    public WatchEventType Type { get; set; }
    public TResource Object { get; set; }
}

public class ResourceInformer<TResource>(
    IKubernetes client,
    string? ns,
    IIndexer<TResource> indexer,
    TimeSpan? resyncPeriod = null)
    : IInformer<TResource>, IInternalInformer
    where TResource : IKubernetesObject<V1ObjectMeta>
{

    private readonly TimeSpan _resyncPeriod = resyncPeriod ?? TimeSpan.FromMinutes(10);

    public event Action<TResource, CancellationToken>? OnAdd;
    public event Action<TResource?, TResource, CancellationToken>? OnUpdate;
    public event Action<TResource, CancellationToken>? OnDelete;

    public IIndexer<TResource> Indexer => indexer;
    public IEnumerable<TResource> List() => indexer.List();

    private readonly KubernetesEntityAttribute _entityInfo = typeof(TResource).GetCustomAttribute<KubernetesEntityAttribute>()
            ?? throw new InvalidOperationException($"Type {typeof(TResource).Name} must have KubernetesEntityAttribute");

    private volatile bool _synced;
    private string? _lastResourceVersion;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var list = await ListAsync(cancellationToken);
        indexer.Replace(list.Items);
        _lastResourceVersion = list.ResourceVersion();
        _synced = true;

        foreach (var item in list.Items)
        {
            OnAdd?.Invoke(item, cancellationToken);
        }

        _ = Task.Run(() => WatchLoop(cancellationToken), cancellationToken);
    }

    public Task<bool> WaitForSyncAsync(CancellationToken cancellationToken)
        => Task.FromResult(_synced);

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private async Task<KubernetesList<TResource>> ListAsync(CancellationToken cancellationToken)
    {
        if (ns != null)
        {
            return await client.CustomObjects.ListNamespacedCustomObjectAsync<KubernetesList<TResource>>(
                group: _entityInfo.Group,
                version: _entityInfo.ApiVersion,
                plural: _entityInfo.PluralName,
                namespaceParameter: ns,
                allowWatchBookmarks: true,
                labelSelector: "",
                cancellationToken: cancellationToken
                );
        }
        else
        {
            return await client.CustomObjects.ListClusterCustomObjectAsync<KubernetesList<TResource>>(
                group: _entityInfo.Group,
                version: _entityInfo.ApiVersion,
                plural: _entityInfo.PluralName,
                allowWatchBookmarks: true,
                labelSelector: "",
                cancellationToken: cancellationToken
            );
        }
    }
    private async Task WatchLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var watcher = GetWatchStream(cancellationToken);
                await foreach (var evt in watcher)
                {
                    // Update last seen resource version
                    if (evt.Object?.Metadata?.ResourceVersion != null)
                    {
                        _lastResourceVersion = evt.Object.Metadata.ResourceVersion;
                    }

                    if (evt.Object is null) continue;

                    switch (evt.Type)
                    {
                        case WatchEventType.Added:
                            indexer.AddOrUpdate(evt.Object);
                            OnAdd?.Invoke(evt.Object, cancellationToken);
                            break;
                        case WatchEventType.Modified:
                            var oldObj = indexer.Get(evt.Object);
                            indexer.AddOrUpdate(evt.Object);
                            OnUpdate?.Invoke(oldObj, evt.Object, cancellationToken);
                            break;
                        case WatchEventType.Deleted:
                            indexer.Delete(evt.Object);
                            OnDelete?.Invoke(evt.Object, cancellationToken);
                            break;
                        default:
                            // Ignore unknown event types
                            break;
                    }
                }
            }
            catch when (!cancellationToken.IsCancellationRequested)
            {
                // Log and retry watch on error
                await Task.Delay(_resyncPeriod, cancellationToken);
            }
        }
    }

    private async IAsyncEnumerable<WatchEvent<TResource>> GetWatchStream([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<(WatchEventType, object)> watchStream;

        if (ns != null)
        {
            // Watch namespace-scoped resources
            watchStream = client.CustomObjects.WatchListNamespacedCustomObjectAsync(
                group: _entityInfo.Group,
                version: _entityInfo.ApiVersion,
                plural: _entityInfo.PluralName,
                namespaceParameter: ns,
                allowWatchBookmarks: true,
                resourceVersion: _lastResourceVersion,
                labelSelector: "",
                timeoutSeconds: (int)TimeSpan.FromMinutes(60).TotalSeconds,
                cancellationToken: cancellationToken);
        }
        else
        {
            // Watch cluster-scoped resources
            watchStream = client.CustomObjects.WatchListClusterCustomObjectAsync(
                group: _entityInfo.Group,
                version: _entityInfo.ApiVersion,
                plural: _entityInfo.PluralName,
                allowWatchBookmarks: true,
                resourceVersion: _lastResourceVersion,
                labelSelector: "",
                timeoutSeconds: (int)TimeSpan.FromMinutes(60).TotalSeconds,
                cancellationToken: cancellationToken);
        }

        await foreach (var (type, item) in watchStream)
        {
            yield return new()
            {
                Type = type,
                Object = KubernetesJson.Deserialize<TResource>(((JsonElement)item).GetRawText())
            };
        }
    }
}
