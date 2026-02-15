using k8s;
using k8s.Models;
using Simplicity.Operator.Controllers;
using Simplicity.Operator.Informer;
using System.Collections.Concurrent;

namespace Simplicity.Operator.Reconciler;

public class ReconcilerFactory(SharedInformerFactory informerFactory, IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<Type, IReconciler> _reconcilers = new();
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public IReconciler Create<T>(ReconcileDelegate<T> reconcile)
        where T : IKubernetesObject<V1ObjectMeta>
    {
        var informer = informerFactory.GetInformer<T>();
        return _reconcilers.GetOrAdd(typeof(T), _ =>
        {
            var queue = new ChannelWorkQueue<T>();
            var reconciler = new Reconciler<T>(informer, queue, _serviceProvider, reconcile);
            return reconciler;
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = _reconcilers.Values.Select(i => i.StartAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _reconcilers.Values.Select(i => i.StopAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }
}
