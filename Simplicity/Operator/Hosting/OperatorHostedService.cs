using k8s;
using k8s.LeaderElection;
using k8s.LeaderElection.ResourceLock;
using Simplicity.Operator.Configuration;
using Simplicity.Operator.Informer;
using Simplicity.Operator.Reconciler;

namespace Simplicity.Operator.Hosting;

public class OperatorHostedService : IHostedService
{
    private readonly IKubernetes _client;
    private readonly ILogger<OperatorHostedService> _logger;
    private readonly SharedInformerFactory _informerFactory;
    private readonly ReconcilerFactory _reconcilerFactory;
    private readonly LeaderElector _leaderElector;
    private readonly OperatorConfiguration _configuration;
    private Task? _leaderElectionTask;
    private CancellationTokenSource? _leaderElectionCts;

    public OperatorHostedService(
        IKubernetes client,
        ILogger<OperatorHostedService> logger,
        SharedInformerFactory informerFactory,
        ReconcilerFactory reconcilerFactory,
        OperatorConfiguration config)
    {
        _client = client;
        _logger = logger;
        _informerFactory = informerFactory;
        _reconcilerFactory = reconcilerFactory;
        _configuration = config;

        var leaseLock = new LeaseLock(
            client: _client,
            @namespace: config.Namespace,
            name: config.Lease.LeaseName,
            identity: Environment.MachineName
        );

        var leaderElectionConfig = new LeaderElectionConfig(leaseLock)
        {
            LeaseDuration = config.Lease.LeaseDuration,
            RenewDeadline = config.Lease.RenewDeadline,
            RetryPeriod = config.Lease.RetryPeriod
        };

        _leaderElector = new LeaderElector(leaderElectionConfig);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {operator} - {version} hosted service - beginning leader election", _configuration.Name, _configuration.Version);
        _leaderElector.OnStartedLeading += OnStartedLeading;
        _leaderElector.OnStoppedLeading += OnStoppedLeading;
        _leaderElector.OnNewLeader += OnNewLeader;

        _leaderElectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _leaderElectionTask = _leaderElector.RunAndTryToHoldLeadershipForeverAsync(_leaderElectionCts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_leaderElectionCts != null)
        {
            _leaderElectionCts.Cancel();
        }

        if (_leaderElectionTask != null)
        {
            try
            {
                await _leaderElectionTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _leaderElectionCts?.Dispose();
    }

    private void OnStartedLeading()
    {
        _logger.LogInformation("Started leading - initializing operator components");

        Task.Run(async () =>
        {
            try
            {
                var token = _leaderElectionCts?.Token ?? CancellationToken.None;
                await _informerFactory.StartAsync(token);
                await _informerFactory.WaitForCacheSyncAsync(token);
                await _reconcilerFactory.StartAsync(token);
                _logger.LogInformation("Operator components started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start operator components");
                _leaderElectionCts?.Cancel();
            }
        });
    }

    private void OnStoppedLeading()
    {
        _logger.LogInformation("Stopped leading - shutting down operator components");

        Task.Run(async () =>
        {
            try
            {
                await _reconcilerFactory.StopAsync(CancellationToken.None);
                await _informerFactory.StopAsync(CancellationToken.None);
                _logger.LogInformation("Operator components stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop operator components");
            }
        });
    }

    private void OnNewLeader(string identity)
    {
        if (identity == Environment.MachineName)
        {
            _logger.LogInformation("This instance is now the leader");
        }
        else
        {
            _logger.LogInformation("New leader elected: {identity}", identity);
        }
    }
}
