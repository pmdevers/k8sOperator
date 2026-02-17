using k8s.Operator;
using k8s.Operator.Generation;
using k8s.Operator.Reconciler;

namespace simple_operator.Features.Applications;

public static class Reconciler
{
    extension(IHost app)
    {
        public void MapMyAppReconciler() =>
            app.AddReconciler<V1MyApp>(ReconcileAsync);
    }


    public static async Task ReconcileAsync(ReconcileContext<V1MyApp> context)
    {
        context.Logger.LogInformation("Reconciling MyApp {Name} in namespace {Namespace}",
            context.Resource.Metadata.Name, context.Resource.Metadata.NamespaceProperty);

        // Your reconciliation logic here
        // You can access other resources from the cache
        var allApps = context.Informer.List().Count();

        context.Logger.LogInformation("Total apps in cache: {Count}", allApps);

        context.Update(x =>
        {
            x.WithLabel("managed-by", "simple-operator");
            x.WithLabel("processed", "true");
            x.WithStatus(x =>
            {
                x.Phase = "Reconciling";
                x.ReadyReplicas = context.Resource.Spec?.Replicas ?? 0;
            });
        });
    }
}
