using k8s.Operator;

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

        await context.Update<Provider>()
            .AddLabel("managed-by", context.Configuration.Name)
            .AddLabel("processed", "true")
            .AddAnnotation("last-reconcile", DateTime.UtcNow.ToString("o"))
            .ApplyAsync();

        var fileprovider = (provider.Spec.File?.Create() ?? provider.Spec.AzureBlob?.Create());

        if (fileprovider is not null)
        {
            var dirs = fileprovider.GetDirectoryContents("")
                .Where(x => x.IsDirectory)
                .Select(x => x.Name).ToList();

            await context.Update<Provider>()
            .WithStatus(x =>
            {
                x.Status ??= new Provider.State();
                x.Status.NumberOfReleases = dirs.Count;
                x.Status.Versions = dirs.TakeLast(10).ToArray();
            })
            .ApplyAsync();
        }
    }
}
