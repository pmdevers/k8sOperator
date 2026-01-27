namespace k8s.Operator.Informer;

public interface IInformerInternal
{
    Task StartAsync(CancellationToken cancellationToken);
    Task<bool> WaitForSyncAsync(CancellationToken cancellationToken);
}
