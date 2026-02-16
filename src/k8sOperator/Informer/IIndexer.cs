using k8s;
using k8s.Models;
using System.Collections.Concurrent;

namespace k8s.Operator.Informer;

public interface IIndexer<T>
    where T : IKubernetesObject<V1ObjectMeta>
{
    void AddOrUpdate(T item);
    void Delete(T item);
    IEnumerable<T> List();
    T? Get(T obj);
    void Replace(IEnumerable<T> items);
}

public class InMemoryIndexer<T> : IIndexer<T>
    where T : IKubernetesObject<V1ObjectMeta>
{
    private readonly ConcurrentDictionary<ResourceKey, T> _set = new();
    public void AddOrUpdate(T item)
    {
        var key = ResourceKey.Create(item);
        _set[key] = item;
    }
    public void Delete(T item)
    {
        var key = ResourceKey.Create(item);
        _set.TryRemove(key, out _);
    }
    public IEnumerable<T> List() => _set.Values;

    public void Replace(IEnumerable<T> items)
    {
        _set.Clear();
        foreach (var item in items)
            AddOrUpdate(item);
    }

    public T? Get(T item)
    {
        var key = ResourceKey.Create(item);
        return _set.TryGetValue(key, out var value) ?
            value : default;
    }
}

public record struct ResourceKey(string Name, string? Namespace)
{
    public static ResourceKey Create(IKubernetesObject<V1ObjectMeta> obj)
        => new(obj.Metadata.Name, obj.Metadata.NamespaceProperty ?? string.Empty);
}
