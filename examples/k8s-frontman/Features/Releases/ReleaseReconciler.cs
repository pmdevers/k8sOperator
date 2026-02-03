using k8s.Operator;

namespace k8s.Frontman.Features.Releases;

public static class ReleaseReconciler
{
    public static Task ReconcileAsync(OperatorContext context)
    {
        return Task.CompletedTask;
    }
}
