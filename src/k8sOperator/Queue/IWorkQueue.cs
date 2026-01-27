namespace k8s.Operator.Queue;

public interface IWorkQueue<TItem>
{
    ValueTask EnqueueAsync(TItem item, CancellationToken cancellationToken);
    ValueTask<TItem> DequeueAsync(CancellationToken cancellationToken);
    ValueTask Requeue(TItem item, TimeSpan? delay = null, CancellationToken cancellationToken = default);
}
