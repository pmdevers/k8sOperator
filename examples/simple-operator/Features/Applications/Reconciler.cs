using k8s.Operator;
using k8s.Operator.Generation;

namespace simple_operator.Features.ManageApplication;

public static class Reconciler
{
    extension(IHost app)
    {
        public void MapMyAppReconciler() =>
            app.ReconcilerFor<MyApp>(ReconcileAsync);
    }


    public static async Task ReconcileAsync(OperatorContext context, ILogger<MyApp> logger)
    {
        var informer = context.GetInformer<MyApp>();
        var key = context.ResourceKey;
        var resource = context.Resource as MyApp;

        if (resource == null)
        {
            logger.LogInformation("Resource {Name} in namespace {Namespace} was deleted",
                key.Name, key.Namespace);
            // Handle deletion
            return;
        }

        logger.LogInformation("Reconciling MyApp {Name} in namespace {Namespace}",
            resource.Metadata.Name, resource.Metadata.NamespaceProperty);

        // Your reconciliation logic here
        // You can access other resources from the cache
        var allApps = informer.List();
        logger.LogInformation("Total apps in cache: {Count}", allApps.Count);

        await context.Update<MyApp>(x =>
        {
            x.WithLabel("managed-by", "simple-operator");
            x.WithLabel("processed", "true");
            x.WithLabel("last-reconcile", DateTime.UtcNow.ToString("o"));
        });

        await context.Update<MyApp>(x =>
            x.WithStatus(x =>
            {
                x.Phase = "Reconciling";
                x.ReadyReplicas = resource.Spec?.Replicas ?? 0;
            }));

        logger.LogInformation("Reconciled MyApp {Name}, replicas: {Replicas}",
            resource.Metadata.Name, resource.Status?.ReadyReplicas);
    }
}
