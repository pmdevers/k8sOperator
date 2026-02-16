using k8s.Models;
using k8s.Operator.Informer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace k8s.Operator.Reconciler;

public delegate Task ReconcileDelegate<T>(ReconcileContext<T> ctx)
    where T : IKubernetesObject<V1ObjectMeta>;

public class Reconciler<T> : IReconciler
    where T : IKubernetesObject<V1ObjectMeta>
{
    private readonly ReconcileDelegate<T> _reconcile;
    private CancellationTokenSource? _cts;

    public Reconciler(
        IInformer<T> informer,
        IWorkQueue<T> queue,
        IServiceProvider serviceProvider,
        ReconcileDelegate<T> reconcile)
    {
        informer.OnAdd += async (w, ct) => await queue.EnqueueAsync(w, ct);
        informer.OnUpdate += async (oldW, newW, ct) => await queue.EnqueueAsync(newW, ct);
        informer.OnDelete += async (w, ct) => await queue.EnqueueAsync(w, ct);
        Informer = informer;
        Queue = queue;
        Logger = serviceProvider.GetRequiredService<ILogger<T>>();
        Services = serviceProvider;
        _reconcile = reconcile;

    }
    public IInformer<T> Informer { get; }
    public IWorkQueue<T> Queue { get; }
    public ILogger<T> Logger { get; }
    public IServiceProvider Services { get; }
    public async Task StartAsync(CancellationToken token)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        Logger.LogInformation("Reconciler started.");

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var resource = await Queue.DequeueAsync(_cts.Token);
                using var scope = Services.CreateScope();
                var context = new ReconcileContext<T>(scope.ServiceProvider, Informer, Queue, resource, _cts.Token);
                await _reconcile(context);
            }
            catch (OperationCanceledException)
            {

            }
        }

        Logger.LogInformation("Reconciler stopped.");
    }

    public Task StopAsync(CancellationToken token)
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
