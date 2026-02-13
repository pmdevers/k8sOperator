using k8s.Operator;

namespace simple_operator.Features.Todos;

public static class Reconciler
{
    extension(IHost app)
    {
        public void MapTodoItemReconciler() =>
            app.ReconcilerFor<TodoItem>(ReconcileAsync);
    }

    public static async Task ReconcileAsync(OperatorContext context, ILogger<TodoItem> logger)
    {
        var informer = context.GetInformer<TodoItem>();
        var key = context.ResourceKey;
        if (context.Resource is not TodoItem resource)
        {
            logger.LogInformation("TodoItem '{Name}' in namespace '{Namespace}' was deleted.",
                key.Name, key.Namespace ?? "default");
            return;
        }
        logger.LogInformation("Reconciling TodoItem {Name} in namespace {Namespace} with title {Title}",
            resource.Metadata.Name,
            resource.Metadata.NamespaceProperty,
            resource.Spec.Title);


        if (resource.Status is null || resource.Status.ReconciliationCount == 0)
            await context.Update<TodoItem>(x =>
            {
                x.WithStatus(x =>
                {
                    x.State = "in-progress";
                    x.Message = "Todo item is being processed.";
                    x.ReconciliationCount++;
                });
            });

        if (resource.Status?.ReconciliationCount <= 3)
        {
            await context.Update<TodoItem>(x =>
            {
                x.WithStatus(x =>
                {
                    x.State = "completed";
                    x.CompletedAt = DateTime.UtcNow;
                    x.Message = "Todo item has been completed.";
                    x.ReconciliationCount++;
                });
            });
        }
        else
        {
            await context.Update<TodoItem>(x =>
            {
                x.WithStatus(x => x.Message = $"Todo item has been reconciled {resource.Status?.ReconciliationCount} times.");
            });
        }


        logger.LogInformation("Updated status for TodoItem {Name}: State={State}, Message={Message}",
        resource.Metadata.Name,
        resource.Status?.State,
        resource.Status?.Message);
    }
}
