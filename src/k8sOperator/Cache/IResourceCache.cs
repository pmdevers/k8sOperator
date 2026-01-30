using k8s.Operator.Models;

namespace k8s.Operator.Cache;

/// <summary>
/// Represents a cache for Kubernetes resources of type <typeparamref name="TResource"/>.
/// </summary>
/// <typeparam name="TResource">The type of the resource being cached.</typeparam>
public interface IResourceCache<TResource>
    where TResource : CustomResource
{
    /// <summary>
    /// Lists all resources currently in the cache.
    /// </summary>
    /// <returns>A read-only list of resources.</returns>
    IReadOnlyList<TResource> List();

    /// <summary>
    /// Gets a resource by name and optional namespace.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="ns">The namespace of the resource (optional).</param>
    /// <returns>The resource if found; otherwise, <c>null</c>.</returns>
    TResource? Get(string name, string? ns = null);

    /// <summary>
    /// Replaces the contents of the cache with the specified items.
    /// </summary>
    /// <param name="items">The items to replace the cache with.</param>
    void Replace(IEnumerable<TResource> items);

    /// <summary>
    /// Applies a watch event to the cache, updating it according to the event type and object.
    /// </summary>
    /// <param name="watcher">The watch event to apply.</param>
    void Apply(WatchEvent<TResource> watcher);
}
