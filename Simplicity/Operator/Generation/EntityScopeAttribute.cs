namespace Simplicity.Operator.Generation;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class EntityScopeAttribute(EntityScope scope) : Attribute
{
    public string Scope { get; } = scope.ToString();
    public static readonly EntityScopeAttribute Default = new(EntityScope.Namespaced);
}

/// <summary>
/// 
/// </summary>
public enum EntityScope
{
    /// <summary>
    /// The entity is scoped to a specific namespace.
    /// </summary>
    Namespaced = 0,

    /// <summary>
    /// The entity is scoped to the entire cluster.
    /// </summary>
    Cluster = 1
}
