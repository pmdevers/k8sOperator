using k8s.Frontman.Features.Providers;
using k8s.Operator;

namespace k8s.Frontman.Features.Releases;

public static class ReleaseReconciler
{
    public static async Task ReconcileAsync(OperatorContext context)
    {
        var newVersion = context.Resource as Release;
        var infromer = context.GetInformer<Release>();
        var providers = context.GetInformer<Provider>();

        if (newVersion == null)
        {
            return;
        }

        var current = infromer.Get(newVersion.Metadata.Name, newVersion.Metadata.NamespaceProperty);
        var provider = providers.Get(newVersion.Spec.Provider, newVersion.Metadata.NamespaceProperty);

        if (provider is null)
        {
            await context.Update<Release>()
            .WithStatus(x =>
            {
                x.Status = x.Status ?? new();
                x.Status.Message = $"Provider '{newVersion.Spec.Provider}' not found.";
            }).ApplyAsync();

            return;
        }

        if (!provider.Status!.Versions.Contains(newVersion.Spec.Version))
        {
            await context.Update<Release>()
            .WithStatus(x =>
            {
                x.Status = x.Status ?? new();
                x.Status.Message = $"Version '{newVersion.Spec.Version}' not found.";
            }).ApplyAsync();

            return;
        }

        await context.Update<Release>()
            .AddLabel("frontman.io/release-provider", newVersion.Spec.Provider)
            .ApplyAsync();


        await context.Update<Release>()
            .WithStatus(x =>
            {
                x.Status = x.Status ?? new();
                x.Status.PreviousVersion = current?.Status?.CurrentVersion ?? string.Empty;
                x.Status.CurrentVersion = newVersion.Spec.Version;
                x.Status.Message = string.Empty;
            }).ApplyAsync();
    }
}
