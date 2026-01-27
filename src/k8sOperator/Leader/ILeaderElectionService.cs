namespace k8s.Operator.Leader;

public interface ILeaderElectionService
{
    bool IsLeader { get; }
    Task StartAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Waits until leadership is acquired.
    /// </summary>
    Task WaitForLeadershipAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Waits until leadership is lost.
    /// </summary>
    Task WaitForLeadershipLostAsync(CancellationToken cancellationToken);
}
