using System.Threading.Channels;

namespace k8s.Operator.Reconciler;

public class ChannelWorkQueue<T> : IWorkQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
    public ValueTask EnqueueAsync(T item, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(item, cancellationToken);
    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken);
    public async ValueTask Requeue(T item, TimeSpan? delay = null, CancellationToken cancellationToken = default)
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

