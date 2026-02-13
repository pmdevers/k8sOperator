using k8s.Frontman.Features.Releases;
using k8s.Operator;
using k8s.Operator.Generation;

namespace k8s.Frontman.Features.Providers;

public static partial class ProviderReconciler
{
    public static async Task ReconcileAsync(OperatorContext context)
    {
        var informer = context.GetInformer<Provider>();
        var key = context.ResourceKey;

        if (context.Resource is not Provider provider)
        {
            return;
        }

        await context.Update<Provider>(x =>
        {

            x.WithLabel("managed-by", context.Configuration.Name);
            x.WithLabel("processed", "true");
            x.WithAnnotation("last-reconcile", DateTime.UtcNow.ToString("o"));
        });

        var fileprovider = (provider.Spec.File?.Create() ?? provider.Spec.AzureBlob?.Create());

        if (fileprovider is not null)
        {
            var dirs = fileprovider.GetDirectoryContents("")
                .Where(x => x.IsDirectory)
                .Select(x => x.Name).ToList();

            await context.Update<Provider>(x =>
            {
                x.WithStatus(x =>
                {
                    x.NumberOfReleases = dirs.Count;
                    x.Versions = [.. dirs.TakeLast(10)];
                });
            });
        }
    }
}
