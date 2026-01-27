using k8s.Models;
using k8s.Operator;
using k8s.Operator.Builders;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace k8s.Operator.Controller;

public class InformerController<TResource> : IController
    where TResource : IKubernetesObject<V1ObjectMeta>
{
    private readonly IInformer<TResource> _informer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkQueue<ResourceKey> _queue;
    private readonly ReconcileDelegate _reconcile;
    private readonly ILogger _logger;

    public InformerController(
        IServiceProvider serviceProvider,
        IInformerFactory informerFactory,
        IWorkQueue<ResourceKey> queue,
        ReconcileDelegate reconcile,
        ILogger logger)
    {
        _informer = informerFactory.GetInformer<TResource>();
        _serviceProvider = serviceProvider;
        _queue = queue;
        _reconcile = reconcile;
        _logger = logger;
    }
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => PumpEventsAsync(cancellationToken), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var key = await _queue.DequeueAsync(cancellationToken);
            try
            {
                using var scope = _serviceProvider.CreateAsyncScope();
                {
                    var resource = _informer.Get(name: key.Name, ns: key.Namespace);

                var context = new OperatorContext
                {
                    RequestServices = scope.ServiceProvider,
                    ResourceKey = key,
                    Resource = resource,
                };

                if (_reconcile != null)
                {
                    await _reconcile.Invoke(context);
                }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling resource {ResourceKey}", key);
                await _queue.Requeue(key, TimeSpan.FromSeconds(5), cancellationToken);
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
            _logger.LogDebug("Enqueuing resource {ResourceKey} due to event {EventType}", key, evt.Type);
            await _queue.EnqueueAsync(key, cancellationToken);
        }
    }

}
