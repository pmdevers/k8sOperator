using k8s.Operator;
using k8s.Operator.Reconciler;

namespace simple_operator.Features.Todos;

public static class Reconciler
{
    extension(IHost app)
    {
        public void MapTodoItemReconciler() =>
            app.AddReconciler<V1TodoItem>(ReconcileAsync);
    }

    public static async Task ReconcileAsync(ReconcileContext<V1TodoItem> context)
    {
        if (context.Resource.Status is null || context.Resource.Status.ReconciliationCount == 0)
            context.Update(x =>
            {
                x.WithStatus(x =>
                {
                    x.State = "in-progress";
                    x.Message = "Todo item is being processed.";
                    x.ReconciliationCount++;
                });
            });

        if (context.Resource.Status?.ReconciliationCount <= 3)
        {
            context.Update(x =>
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
            context.Update(x =>
            {
                x.WithStatus(x => x.Message = $"Todo item has been reconciled {context.Resource.Status?.ReconciliationCount} times.");
            });
        }
    }
}
