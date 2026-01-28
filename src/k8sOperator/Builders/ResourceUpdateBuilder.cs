using k8s.Operator.Models;
using Microsoft.Extensions.Logging;

namespace k8s.Operator.Builders;

public class ResourceUpdateBuilder<TResource> where TResource : CustomResource
{
    private readonly IKubernetes _kubernetes;
    private readonly TResource _resource;
    private readonly ILogger<TResource> _logger;
    private readonly List<Action<TResource>> _updates = [];
    private bool _updateStatus;

    public ResourceUpdateBuilder(IKubernetes kubernetes, TResource resource)
    {
        _kubernetes = kubernetes;
        _resource = resource;
    }

    public ResourceUpdateBuilder<TResource> WithSpec(Action<TResource> update)
    {
        _updates.Add(update);
        return this;
    }

    public ResourceUpdateBuilder<TResource> WithStatus(Action<TResource> update)
    {
        _updates.Add(update);
        _updateStatus = true;
        return this;
    }

    public ResourceUpdateBuilder<TResource> AddLabel(string key, string value)
    {
        _updates.Add(r =>
        {
            r.Metadata.Labels ??= new Dictionary<string, string>();
            r.Metadata.Labels[key] = value;
        });
        return this;
    }

    public ResourceUpdateBuilder<TResource> AddAnnotation(string key, string value)
    {
        _updates.Add(r =>
        {
            r.Metadata.Annotations ??= new Dictionary<string, string>();
            r.Metadata.Annotations[key] = value;
        });
        return this;
    }

    public ResourceUpdateBuilder<TResource> AddFinalizer(string finalizer)
    {
        _updates.Add(r =>
        {
            r.Metadata.Finalizers ??= new List<string>();
            if (!r.Metadata.Finalizers.Contains(finalizer))
            {
                r.Metadata.Finalizers.Add(finalizer);
            }
        });
        return this;
    }

    public ResourceUpdateBuilder<TResource> RemoveFinalizer(string finalizer)
    {
        _updates.Add(r =>
        {
            r.Metadata.Finalizers?.Remove(finalizer);
        });
        return this;
    }

    public async Task<TResource> ApplyAsync(CancellationToken cancellationToken = default)
    {
        var crd = _resource.GetDefinition<TResource>();

        // Get the LATEST version from the API server (not cache)
        var latest = await _kubernetes.CustomObjects.GetNamespacedCustomObjectAsync<TResource>(
            group: crd.Group,
            version: crd.ApiVersion,
            namespaceParameter: _resource.Metadata.NamespaceProperty,
            plural: crd.PluralName,
            name: _resource.Metadata.Name,
            cancellationToken: cancellationToken);

        foreach (var update in _updates)
        {
            update(latest);
        }

        if (_updateStatus)
        {

            return await _kubernetes.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync<TResource>(
                body: latest,
                group: crd.Group,
                version: crd.ApiVersion,
                plural: crd.PluralName,
                name: _resource.Metadata.Name,
                namespaceParameter: _resource.Metadata.NamespaceProperty,
                cancellationToken: cancellationToken);
        }

        return await _kubernetes.CustomObjects.ReplaceNamespacedCustomObjectAsync<TResource>(
            body: latest,
            group: crd.Group,
            version: crd.ApiVersion,
            plural: crd.PluralName,
            name: _resource.Metadata.Name,
            namespaceParameter: _resource.Metadata.NamespaceProperty,
            cancellationToken: cancellationToken);
    }
}
