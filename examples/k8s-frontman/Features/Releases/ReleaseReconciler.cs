using k8s.Frontman.Features.Providers;
using k8s.Models;
using k8s.Operator.Generation;
using k8s.Operator.Reconciler;

namespace k8s.Frontman.Features.Releases;

public static class ReleaseReconciler
{
    public static async Task ReconcileAsync(ReconcileContext<V1Release> context)
    {
        var informer = context.GetInformer<V1Release>();
        var providers = context.GetInformer<V1Provider>();

        var current = informer.Indexer.Get(context.Resource);
        var provider = providers.Indexer.Get(context.Resource.Spec.Provider, context.Resource.Metadata.NamespaceProperty);

        context.Update(x =>
        {
            x.WithLabel("managed-by", context.Configuration.Name);
            x.WithLabel("processed", "true");
            x.WithLabel($"{context.Resource.ApiGroup()}/provider", context.Resource.Spec.Provider);
        });

        if (provider is null)
        {
            context.Update(x =>
            {
                x.WithStatus(x =>
                {
                    x.Message = $"Provider '{context.Resource.Spec.Provider}' not found.";
                });
            });

            return;
        }

        if (!provider.Status!.Versions.Contains(context.Resource.Spec.Version))
        {
            context.Update(x =>
            {
                x.WithStatus(x =>
                {
                    x.Message = $"Version '{context.Resource.Spec.Version}' not found.";
                    if (current?.Status?.CurrentVersion != context.Resource.Spec.Version)
                    {
                        x.PreviousVersion = current?.Status?.CurrentVersion ?? string.Empty;
                    }
                });
            });

            await context.Queue.Requeue(context.Resource, TimeSpan.FromSeconds(30), context.CancellationToken);

            return;
        }

        context.Update(x =>
        {
            x.WithStatus(x =>
            {
                x.Message = string.Empty;
                x.CurrentVersion = context.Resource.Spec.Version;
                if (current?.Status?.CurrentVersion != context.Resource.Spec.Version)
                {
                    x.PreviousVersion = current?.Status?.CurrentVersion ?? string.Empty;
                }
            });
        });
    }
}
