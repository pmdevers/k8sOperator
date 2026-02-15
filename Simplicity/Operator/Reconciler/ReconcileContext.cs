using k8s;
using k8s.Models;
using Simplicity.Operator.Informer;

namespace Simplicity.Operator.Reconciler;

public class ReconcileContext<T>(
    IServiceProvider serviceProvider,
    IInformer<T> informer,
    IWorkQueue<T> queue,
    T item,
    CancellationToken cancellationToken)
    where T : IKubernetesObject<V1ObjectMeta>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public T Item { get; } = item;
    public IInformer<T> Informer => informer;
    public IWorkQueue<T> Queue => queue;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
