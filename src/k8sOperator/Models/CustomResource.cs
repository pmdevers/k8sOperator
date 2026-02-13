using k8s.Models;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace k8s.Operator.Models;

/// <summary>
/// Represents a custom resource in Kubernetes, inheriting from <see cref="KubernetesObject"/> and implementing <see cref="IKubernetesObject{V1ObjectMeta}"/>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract class CustomResource : KubernetesObject, IKubernetesObject<V1ObjectMeta>
{
    public static KubernetesEntityAttribute GetDefinition<TResource>()
        where TResource : CustomResource
    {
        return typeof(TResource).GetCustomAttribute<KubernetesEntityAttribute>()!;
    }

    /// <inheritdoc />
    [JsonPropertyName("metadata")]
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
}

/// <summary>
/// Represents a custom resource in Kubernetes with a specific specification type.
/// Inherits from <see cref="CustomResource"/> and implements <see cref="ISpec{TSpec}"/>.
/// </summary>
/// <typeparam name="TSpec">The type of the specification.</typeparam>
public abstract class CustomResource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TSpec> : CustomResource, ISpec<TSpec>
    where TSpec : new()
{
    /// <summary>
    /// Gets or sets the specifications for the custom resource.
    /// </summary>
    [JsonPropertyName("spec")]
    public TSpec Spec { get; set; } = new();
}

/// <summary>
/// Represents a custom resource in Kubernetes with specific specification and status types.
/// Inherits from <see cref="CustomResource{TSpec}"/> and implements <see cref="IStatus{TStatus}"/>.
/// </summary>
/// <typeparam name="TSpec">The type of the specification.</typeparam>
/// <typeparam name="TStatus">The type of the status.</typeparam>
public abstract class CustomResource<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TSpec,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TStatus> : CustomResource<TSpec>, IStatus<TStatus?>
    where TSpec : new()
    where TStatus : class
{
    /// <summary>
    /// Gets or sets the status for the custom resource.
    /// </summary>
    [JsonPropertyName("status")]
    public TStatus? Status { get; set; }
}

