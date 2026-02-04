namespace k8s.Operator.Leader;

internal class NoopLeaderElectionService() : ILeaderElectionService
{
    public bool IsLeader => true;
    public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    public Task WaitForLeadershipAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task WaitForLeadershipLostAsync(CancellationToken cancellationToken) => Task.Delay(Timeout.Infinite, cancellationToken);
}

internal class NeverLeaderElectionService() : ILeaderElectionService
{
    public bool IsLeader => true;
    public Task StartAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    public Task WaitForLeadershipAsync(CancellationToken cancellationToken) => Task.Delay(Timeout.Infinite, cancellationToken);
    public Task WaitForLeadershipLostAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
