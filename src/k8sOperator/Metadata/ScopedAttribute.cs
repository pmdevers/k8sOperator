using k8s.Operator.Generation;

namespace k8s.Operator.Metadata;

[AttributeUsage(AttributeTargets.Class)]
public class ScopeAttribute(EntityScope scope) : Attribute
{
    public static ScopeAttribute Default { get; } = new(EntityScope.Namespaced);
    public EntityScope Scope { get; } = scope;
    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(Scope), Scope);
}
