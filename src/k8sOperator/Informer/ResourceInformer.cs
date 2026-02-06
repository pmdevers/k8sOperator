using k8s.Models;
using k8s.Operator;
using k8s.Operator.Cache;
using k8s.Operator.Models;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace k8s.Operator.Informer;

internal class ResourceInformer<TResource> :
IInformer<TResource>, IInformerInternal
where TResource : CustomResource
{
    private readonly IKubernetes _client;
    private readonly string? _namespace;
    private readonly IResourceCache<TResource> _cache;
    private readonly Channel<WatchEvent<TResource>> _events;
    private readonly TimeSpan _resyncPeriod;
    private string? _lastResourceVersion;
    private readonly KubernetesEntityAttribute _entityInfo;

    private volatile bool _synced;

    public ResourceInformer(
        IKubernetes client,
        string? ns,
        IResourceCache<TResource> cache,
        TimeSpan? resyncPeriod = null)
    {
        _client = client;
        _namespace = ns;
        _cache = cache;
        _events = Channel.CreateUnbounded<WatchEvent<TResource>>();
        _resyncPeriod = resyncPeriod ?? TimeSpan.FromMinutes(10);
        _entityInfo = typeof(TResource).GetCustomAttribute<KubernetesEntityAttribute>()
            ?? throw new InvalidOperationException($"Type {typeof(TResource).Name} must have KubernetesEntityAttribute");
    }

    public bool HasSynced => _synced;

    public IAsyncEnumerable<WatchEvent<TResource>> Events => _events.Reader.ReadAllAsync();

    public TResource? Get(string name, string? ns = null)
        => _cache.Get(name, ns);

    public IReadOnlyList<TResource> List()
        => _cache.List();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var list = await ListResourcesAsync(cancellationToken);
        _cache.Replace(list.Items);
        _lastResourceVersion = list.ResourceVersion();
        _synced = true;

        _ = Task.Run(() => WatchLoop(cancellationToken), cancellationToken);
    }

    public Task<bool> WaitForSyncAsync(CancellationToken cancellationToken)
        => Task.FromResult(_synced);

    private async Task<KubernetesList<TResource>> ListResourcesAsync(CancellationToken cancellationToken)
    {
        if (_namespace != null)
        {
            return await _client.CustomObjects.ListNamespacedCustomObjectAsync<KubernetesList<TResource>>(
                group: _entityInfo.Group,
                version: _entityInfo.ApiVersion,
                plural: _entityInfo.PluralName,
                namespaceParameter: _namespace,
                allowWatchBookmarks: true,
                labelSelector: "",
                cancellationToken: cancellationToken
                );
        }
        else
        {
            return await _client.CustomObjects.ListClusterCustomObjectAsync<KubernetesList<TResource>>(
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

                    // Skip status-only updates by checking generation
                    //if (ShouldSkipEvent(evt))
                    //{
                    //    // Still update cache but don't trigger reconciliation
                    //    _cache.Apply(evt);
                    //    continue;
                    //}

                    _cache.Apply(evt);
                    await _events.Writer.WriteAsync(evt, cancellationToken);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Log and retry watch on error
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private bool ShouldSkipEvent(WatchEvent<TResource> evt)
    {
        if (evt.Type == WatchEventType.Deleted)
        {
            return false; // Never skip delete events
        }

        if (evt.Object?.Metadata == null)
        {
            return true;
        }

        // Get cached version to compare
        var cached = _cache.Get(evt.Object.Metadata.Name, evt.Object.Metadata.NamespaceProperty);
        if (cached == null)
        {
            return false; // New resource, don't skip
        }

        // Compare generation - if unchanged, this is likely a status-only update
        var currentGeneration = evt.Object.Metadata.Generation ?? 0;
        var cachedGeneration = cached.Metadata.Generation ?? 0;

        if (currentGeneration == cachedGeneration)
        {
            // Generation unchanged = spec unchanged = status-only update
            return true;
        }

        return false;
    }

    private async IAsyncEnumerable<WatchEvent<TResource>> GetWatchStream([EnumeratorCancellation] CancellationToken cancellationToken)
    {

        IAsyncEnumerable<(WatchEventType, object)> watchStream;

        if (_namespace != null)
        {
            // Watch namespace-scoped resources
            watchStream = _client.CustomObjects.WatchListNamespacedCustomObjectAsync(
                group: _entityInfo.Group,
                version: _entityInfo.ApiVersion,
                plural: _entityInfo.PluralName,
                namespaceParameter: _namespace,
                allowWatchBookmarks: true,
                resourceVersion: _lastResourceVersion,
                labelSelector: "",
                timeoutSeconds: (int)TimeSpan.FromMinutes(60).TotalSeconds,
                cancellationToken: cancellationToken);
        }
        else
        {
            // Watch cluster-scoped resources
            watchStream = _client.CustomObjects.WatchListClusterCustomObjectAsync(
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
