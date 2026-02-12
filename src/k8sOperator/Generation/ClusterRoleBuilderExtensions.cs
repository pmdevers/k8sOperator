using k8s.Models;

namespace k8s.Operator.Generation;

/// <summary>
/// Provides extension methods for building Kubernetes ClusterRoles.
/// </summary>
public static class ClusterRoleBuilderExtensions
{
    /// <summary>
    /// Adds a policy rule to the ClusterRole being built.
    /// </summary>
    /// <param name="builder">The builder instance for the ClusterRole.</param>
    /// <returns>A builder for configuring the policy rule.</returns>
    public static IObjectBuilder<V1PolicyRule> AddRule(this IObjectBuilder<V1ClusterRole> builder)
    {
        var b = ObjectBuilder.Create<V1PolicyRule>();
        builder.Add(x =>
        {
            x.Rules ??= [];
            x.Rules.Add(b.Build());
        });
        return b;
    }
}
