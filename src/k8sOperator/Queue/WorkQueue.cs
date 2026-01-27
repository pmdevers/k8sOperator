using System.Threading.Channels;

namespace k8s.Operator.Queue;

public class WorkQueue<TItem>() : IWorkQueue<TItem>
{
    private readonly Channel<TItem> _channel = Channel.CreateUnbounded<TItem>();
    public ValueTask EnqueueAsync(TItem item, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(item, cancellationToken);
    public async ValueTask<TItem> DequeueAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken);
    public async ValueTask Requeue(TItem item, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        if (delay.HasValue)
            await Task.Delay(delay.Value, cancellationToken);
        await EnqueueAsync(item, cancellationToken);
    }
}
