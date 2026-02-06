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
        if (delay.HasValue && delay.Value > TimeSpan.Zero)
        {
            // Run delay and enqueue in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay.Value, cancellationToken);
                    await EnqueueAsync(item, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Cancelled, don't requeue
                }
            }, cancellationToken);

            return;
        }

        // No delay, enqueue immediately
        await EnqueueAsync(item, cancellationToken);
    }
}
