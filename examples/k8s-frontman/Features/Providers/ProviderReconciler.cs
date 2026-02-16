using k8s.Operator.Generation;
using k8s.Operator.Reconciler;

namespace k8s.Frontman.Features.Providers;

public static partial class ProviderReconciler
{
    public static async Task ReconcileAsync(ReconcileContext<V1Provider> context)
    {
        var fileprovider = context.Resource.GetFileProvider();

        if (fileprovider is not null)
        {
            var dirs = fileprovider.GetDirectoryContents("")
                .Where(x => x.IsDirectory)
                .Select(x => x.Name).ToList();

            context.Update(x =>
            {
                x.WithLabel("managed-by", context.Configuration.Name);
                x.WithLabel("processed", "true");
                x.WithStatus(x =>
                {
                    x.Add(s =>
                    {
                        s.NumberOfReleases = dirs.Count;
                        s.Versions = [.. dirs.TakeLast(10)];
                    });
                });
            });
        }

        await context.Queue.Requeue(context.Resource, ResyncIntervalAttribute.ParseDuration(context.Resource.Spec.Interval));
    }
}
