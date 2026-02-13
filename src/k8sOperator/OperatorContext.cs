using k8s.Operator.Configuration;
using k8s.Operator.Generation;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace k8s.Operator;

public class OperatorContext(IServiceProvider serviceProvider)
{
    private readonly IInformerFactory _informerFactory = serviceProvider.GetRequiredService<IInformerFactory>();
    private readonly IKubernetes _kubernetes = serviceProvider.GetRequiredService<IKubernetes>();
    private readonly IWorkQueue<ResourceKey> _queue = serviceProvider.GetRequiredService<IWorkQueue<ResourceKey>>();

    public IServiceProvider Services { get; } = serviceProvider;
    public required CustomResource? Resource { get; init; }
    public required ResourceKey ResourceKey { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public OperatorConfiguration Configuration => Services.GetRequiredService<OperatorConfiguration>();
    public IKubernetes Kubernetes => _kubernetes;
    public IWorkQueue<ResourceKey> Queue => _queue;

    public IInformer<TResource> GetInformer<TResource>()
        where TResource : CustomResource
    {
        return _informerFactory.GetInformer<TResource>();
    }

    public async Task Update<TResource>(Action<UpdateBuilder<TResource>> update)
        where TResource : CustomResource
    {
        var crd = CustomResource.GetDefinition<TResource>();

        // Get the LATEST version from the API server (not cache)
        var latest = await _kubernetes.CustomObjects.GetNamespacedCustomObjectAsync<TResource>(
            group: crd.Group,
            version: crd.ApiVersion,
            namespaceParameter: Resource.Metadata.NamespaceProperty,
            plural: crd.PluralName,
            name: Resource.Metadata.Name,
            cancellationToken: CancellationToken);

        var builder = new UpdateBuilder<TResource>(latest);
        update(builder);
        var updated = builder.Build();

        if (builder.StatusChanged)
        {
            await _kubernetes.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync(
                body: updated,
                group: crd.Group,
                version: crd.ApiVersion,
                namespaceParameter: Resource.Metadata.NamespaceProperty,
                plural: crd.PluralName,
                name: Resource.Metadata.Name,
                cancellationToken: CancellationToken);
        }

        await _kubernetes.CustomObjects.ReplaceNamespacedCustomObjectAsync<TResource>(
            body: update,
            group: crd.Group,
            version: crd.ApiVersion,
            plural: crd.PluralName,
            name: updated.Metadata.Name,
            namespaceParameter: updated.Metadata.NamespaceProperty,
            cancellationToken: CancellationToken);
    }
}

public class UpdateBuilder<TResource>(TResource resource) : ObjectBuilder<TResource>(resource)
    where TResource : CustomResource
{
    public bool StatusChanged { get; set; } = false;
}
