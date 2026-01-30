using k8s.Operator.Models;
using System.Collections.Concurrent;

namespace k8s.Operator.Cache;

internal class ResourceCache<TResource> : IResourceCache<TResource>
    where TResource : CustomResource
{
    private readonly ConcurrentDictionary<(string ns, string name), TResource> _items = [];

    public IReadOnlyList<TResource> List() => [.. _items.Values];

    public void Apply(WatchEvent<TResource> e)
    {
        switch (e.Type)
        {
            case WatchEventType.Added:
            case WatchEventType.Modified:
                AddOrUpdate(e.Object);
                break;
            case WatchEventType.Deleted:
                Remove(e.Object);
                break;
        }
    }

    public TResource? Get(string name, string? ns = null)
        => _items.TryGetValue((ns ?? string.Empty, name), out var result)
        ? result
        : default;

    public void Replace(IEnumerable<TResource> items)
    {
        _items.Clear();
        foreach (var item in items)
            AddOrUpdate(item);
    }

    private void AddOrUpdate(TResource obj)
    {
        var key = (obj.Metadata.NamespaceProperty ?? string.Empty, obj.Metadata.Name);
        _items[key] = obj;
    }

    private void Remove(TResource obj)
    {
        var key = (obj.Metadata.NamespaceProperty ?? string.Empty, obj.Metadata.Name);
        _items.TryRemove(key, out _);
    }
}
