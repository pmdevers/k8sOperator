using k8s.Operator;
using k8s.Operator.Builders;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace k8s.Operator.Controller;

public class OperatorController<TResource>(
    IServiceProvider serviceProvider,
    IInformerFactory informerFactory,
    IWorkQueue<ResourceKey> queue,
    ReconcileDelegate reconcile,
    IReadOnlyList<object> metadata,
    ILogger logger) : IController
    where TResource : CustomResource
{
    private readonly IInformer<TResource> _informer = informerFactory.GetInformer<TResource>();

    public IReadOnlyList<object> Metadata { get; } = metadata;
    public Type ResourceType { get; } = typeof(TResource);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => PumpEventsAsync(cancellationToken), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var key = await queue.DequeueAsync(cancellationToken);
            try
            {
                using var scope = serviceProvider.CreateAsyncScope();

                var resource = _informer.Get(name: key.Name, ns: key.Namespace);

                var context = new OperatorContext(scope.ServiceProvider)
                {
                    ResourceKey = key,
                    Resource = resource,
                    CancellationToken = cancellationToken,
                };

                if (reconcile != null)
                {
                    await reconcile.Invoke(context);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reconciling resource {ResourceKey}", key);
                await queue.Requeue(key, TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task PumpEventsAsync(CancellationToken cancellationToken)
    {
        await foreach (var evt in _informer.Events.WithCancellation(cancellationToken))
        {
            var key = new ResourceKey(
                Name: evt.Object.Metadata.Name,
                Namespace: evt.Object.Metadata.NamespaceProperty);
            logger.LogDebug("Enqueuing resource {ResourceKey} due to event {EventType}", key, evt.Type);
            await queue.EnqueueAsync(key, cancellationToken);
        }
    }

}
