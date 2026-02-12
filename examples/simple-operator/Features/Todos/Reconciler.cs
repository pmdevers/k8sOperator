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
            await context.Update<TodoItem>()
                .WithStatus(x =>
                {
                    x.Status ??= new();
                    x.Status.State = "in-progress";
                    x.Status.Message = "Todo item is being processed.";
                    x.Status.ReconciliationCount++;
                })
                .ApplyAsync();

        if (resource.Status?.ReconciliationCount <= 3)
        {
            await context.Update<TodoItem>()
                    .WithStatus(x =>
                    {
                        x.Status ??= new();
                        x.Status.State = "completed";
                        x.Status.CompletedAt = DateTime.UtcNow;
                        x.Status.Message = "Todo item has been completed.";
                        x.Status.ReconciliationCount++;
                    })
                    .ApplyAsync();
        }
        else
        {
            await context.Update<TodoItem>()
                    .WithStatus(x =>
                    {
                        x.Status ??= new();
                        x.Status.Message = $"Todo item has been reconciled {resource.Status?.ReconciliationCount} times.";
                    })
                    .ApplyAsync();
        }

        logger.LogInformation("Updated status for TodoItem {Name}: State={State}, Message={Message}",
            resource.Metadata.Name,
            resource.Status?.State,
            resource.Status?.Message);
    }
}
