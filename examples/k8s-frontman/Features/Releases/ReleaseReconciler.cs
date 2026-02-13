using k8s.Frontman.Features.Providers;
using k8s.Models;
using k8s.Operator;
using k8s.Operator.Generation;

namespace k8s.Frontman.Features.Releases;

public static class ReleaseReconciler
{
    public static async Task ReconcileAsync(OperatorContext context)
    {
        var infromer = context.GetInformer<Release>();
        var providers = context.GetInformer<Provider>();

        if (context.Resource is not Release newVersion)
        {
            return;
        }

        var current = infromer.Get(newVersion.Metadata.Name, newVersion.Metadata.NamespaceProperty);
        var provider = providers.Get(newVersion.Spec.Provider, newVersion.Metadata.NamespaceProperty);

        if (provider is null)
        {
            await context.Update<Release>(x =>
            {
                x.WithStatus(x =>
                {
                    x.Message = $"Provider '{newVersion.Spec.Provider}' not found.";
                });
            });

            return;
        }

        await context.Update<Release>(x =>
        {
            x.WithLabel("managed-by", context.Configuration.Name);
            x.WithLabel("processed", "true");
            x.WithLabel($"{newVersion.ApiGroup()}/provider", newVersion.Spec.Provider);
            x.WithAnnotation("last-reconcile", DateTime.UtcNow.ToString("o"));
        });

        if (!provider.Status!.Versions.Contains(newVersion.Spec.Version))
        {
            await context.Update<Release>(x =>
            {
                x.WithStatus(x =>
                {
                    x.Message = $"Version '{newVersion.Spec.Version}' not found.";
                    if (current?.Status?.CurrentVersion != newVersion.Spec.Version)
                    {
                        x.PreviousVersion = current?.Status?.CurrentVersion ?? string.Empty;
                    }
                });
            });

            await context.Queue.Requeue(context.ResourceKey, TimeSpan.FromSeconds(30), context.CancellationToken);

            return;
        }

        await context.Update<Release>(x =>
        {
            x.WithStatus(x =>
            {
                if (current?.Status?.CurrentVersion != newVersion.Spec.Version)
                {
                    x.PreviousVersion = current?.Status?.CurrentVersion ?? string.Empty;
                }

                x.CurrentVersion = newVersion.Spec.Version;
                x.Message = string.Empty;
            });
        });
    }
}
