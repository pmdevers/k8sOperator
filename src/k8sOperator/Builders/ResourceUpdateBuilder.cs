using k8s.Operator.Models;

namespace k8s.Operator.Builders;

public class ResourceUpdateBuilder<TResource>(IKubernetes kubernetes, TResource resource) where TResource
    : CustomResource
{
    private readonly List<Action<TResource>> _updates = [];
    private bool _updateStatus;

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
        var crd = resource.GetDefinition();

        // Get the LATEST version from the API server (not cache)
        var latest = await kubernetes.CustomObjects.GetNamespacedCustomObjectAsync<TResource>(
            group: crd.Group,
            version: crd.ApiVersion,
            namespaceParameter: resource.Metadata.NamespaceProperty,
            plural: crd.PluralName,
            name: resource.Metadata.Name,
            cancellationToken: cancellationToken);

        foreach (var update in _updates)
        {
            update(latest);
        }

        if (_updateStatus)
        {

            return await kubernetes.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync<TResource>(
                body: latest,
                group: crd.Group,
                version: crd.ApiVersion,
                plural: crd.PluralName,
                name: resource.Metadata.Name,
                namespaceParameter: resource.Metadata.NamespaceProperty,
                cancellationToken: cancellationToken);
        }

        return await kubernetes.CustomObjects.ReplaceNamespacedCustomObjectAsync<TResource>(
            body: latest,
            group: crd.Group,
            version: crd.ApiVersion,
            plural: crd.PluralName,
            name: resource.Metadata.Name,
            namespaceParameter: resource.Metadata.NamespaceProperty,
            cancellationToken: cancellationToken);
    }
}
