namespace Simplicity.Operator.Reconciler;

public interface IWorkQueue<T>
{
    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken);
    ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
    ValueTask Requeue(T item, TimeSpan? delay = null, CancellationToken cancellationToken = default);
}

