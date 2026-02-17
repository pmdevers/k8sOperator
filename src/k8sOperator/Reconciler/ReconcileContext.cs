using k8s.Models;
using k8s.Operator.Configuration;
using k8s.Operator.Generation;
using k8s.Operator.Informer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace k8s.Operator.Reconciler;

public class ReconcileContext<T>(
    IServiceProvider serviceProvider,
    IInformer<T> informer,
    IWorkQueue<T> queue,
    T resource,
    CancellationToken cancellationToken)
    where T : IKubernetesObject<V1ObjectMeta>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public T Resource { get; private set; } = resource;
    public IInformer<T> Informer => informer;
    public IWorkQueue<T> Queue => queue;
    public ILogger<T> Logger => ServiceProvider.GetRequiredService<ILogger<T>>();
    public IKubernetes Kubernetes => ServiceProvider.GetRequiredService<IKubernetes>();
    public OperatorConfiguration Configuration => ServiceProvider.GetRequiredService<OperatorConfiguration>();
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public IInformer<TResource> GetInformer<TResource>(string? ns = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var factory = ServiceProvider.GetRequiredService<SharedInformerFactory>();
        return factory.GetInformer<TResource>(ns);
    }

    private readonly List<Action<IObjectBuilder<T>>> _pendingUpdates = [];

    public void Update(Action<IObjectBuilder<T>> builder)
    {
        _pendingUpdates.Add(builder);
    }

    internal async Task SaveChangesAsync()
    {
        var entityInfo = typeof(T).GetCustomAttribute<KubernetesEntityAttribute>() ??
            throw new InvalidOperationException($"Missing {nameof(KubernetesEntityAttribute)} on {typeof(T).Name}");

        try
        {
            var latest = await Kubernetes.CustomObjects.GetNamespacedCustomObjectAsync<T>(
               group: entityInfo.Group,
               version: entityInfo.ApiVersion,
               namespaceParameter: Resource.Metadata.NamespaceProperty,
               plural: entityInfo.PluralName,
               name: Resource.Metadata.Name,
               cancellationToken: CancellationToken);

            var updatedResource = KubernetesObjectBuilder.Create(latest);
            foreach (var builder in _pendingUpdates)
                builder(updatedResource);

            var updated = updatedResource.Build();
            var changes = ResourceChanges.DetectChanges(latest, updated);

            if (changes.HasStatusChanges)
            {
                Logger.LogDebug("Updating status for {ResourceName}", Resource.Name());

                latest = await Kubernetes.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync<T>(
                    body: updated,
                    group: entityInfo.Group,
                    version: entityInfo.ApiVersion,
                    namespaceParameter: updated.Namespace(),
                    plural: entityInfo.PluralName,
                    name: updated.Name(),
                    cancellationToken: CancellationToken
                );
                updatedResource = KubernetesObjectBuilder.Create(latest);
                foreach (var builder in _pendingUpdates)
                    builder(updatedResource);
                updated = updatedResource.Build();
            }

            if (changes.HasSpecOrMetadataChanges)
            {
                await Kubernetes.CustomObjects.ReplaceNamespacedCustomObjectAsync<T>(
                    body: updated,
                    group: entityInfo.Group,
                    version: entityInfo.ApiVersion,
                    namespaceParameter: updated.Namespace(),
                    plural: entityInfo.PluralName,
                    name: updated.Name(),
                    cancellationToken: CancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update status for {ResourceName}", Resource.Name());
        }
    }
}


public record ResourceChanges
{
    public bool HasStatusChanges { get; init; }
    public bool HasSpecOrMetadataChanges { get; init; }

    public static ResourceChanges DetectChanges<T>(T original, T updated)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        var statusProperty = typeof(T).GetProperty("Status");
        var specProperty = typeof(T).GetProperty("Spec");

        var hasStatusChanges = false;
        var hasSpecChanges = false;

        // Check if status changed
        if (statusProperty != null)
        {
            var originalStatus = statusProperty.GetValue(original);
            var updatedStatus = statusProperty.GetValue(updated);

            hasStatusChanges = !AreEqual(originalStatus, updatedStatus);
        }

        // Check if spec changed
        if (specProperty != null)
        {
            var originalSpec = specProperty.GetValue(original);
            var updatedSpec = specProperty.GetValue(updated);

            hasSpecChanges = !AreEqual(originalSpec, updatedSpec);
        }

        // Check if metadata changed (excluding resourceVersion, managedFields, etc.)
        var hasMetadataChanges = HasMetadataChanges(original.Metadata, updated.Metadata);

        return new ResourceChanges
        {
            HasStatusChanges = hasStatusChanges,
            HasSpecOrMetadataChanges = hasSpecChanges || hasMetadataChanges
        };
    }

    private static bool HasMetadataChanges(V1ObjectMeta original, V1ObjectMeta updated)
    {
        var labels = AreEqual(original.Labels, updated.Labels);
        var annotations = AreEqual(original.Annotations, updated.Annotations);

        // Properly handle null finalizers
        var finalizersEqual = (original.Finalizers, updated.Finalizers) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            var (o, u) => o.SequenceEqual(u)
        };

        // Compare relevant metadata fields (ignore runtime fields)
        return !labels || !annotations || !finalizersEqual;
    }

    private static bool AreEqual(object? obj1, object? obj2)
    {
        if (ReferenceEquals(obj1, obj2)) return true;
        if (obj1 is null || obj2 is null) return false;

        try
        {
            var json1 = KubernetesJson.Serialize(obj1);
            var json2 = KubernetesJson.Serialize(obj2);
            return json1 == json2;
        }
        catch
        {
            // Fallback to object equals
            return obj1.Equals(obj2);
        }
    }

}
