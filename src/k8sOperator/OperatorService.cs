using k8s.Operator.Helpers;
using k8s.Operator.Informer;
using k8s.Operator.Leader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace k8s.Operator;

public class OperatorService(
    ILeaderElectionService leaderElectionService,
    IInformerFactory informerFactory,
    ControllerDatasource controllerDatasource,
    ILogger<OperatorService> logger) : IHostedService
{
    private readonly List<Task> _controllerTasks = [];
    private CancellationTokenSource? _stoppingCts;


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() =>
            leaderElectionService.StartAsync(cancellationToken), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Waiting for leadership...");

            await leaderElectionService.WaitForLeadershipAsync(cancellationToken);

            logger.LogInformation("Leadership acquired");

            using var watcherCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await RunAsync(watcherCts.Token);

            await leaderElectionService.WaitForLeadershipLostAsync(cancellationToken);

            logger.LogInformation("Leadership lost, stopping controllers...");

            // Leadership lost or stopping, cancel watchers
            await watcherCts.CancelAsync();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Operator Service...");

        if (_stoppingCts != null)
            await _stoppingCts.CancelAsync();

        if (_controllerTasks.Count > 0)
        {
            await Task.WhenAll(_controllerTasks);
        }

        _stoppingCts?.Dispose();

        logger.LogInformation("Operator Service stopped");
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = new CancellationTokenSource();

        logger.LogInformation("Starting Operator Service...");

        // Build all controllers first (this registers informers)
        var controllers = controllerDatasource.GetControllers().ToList();

        logger.LogInformation("Built {Count} controller(s)", controllers.Count);

        // Now start the informers
        await informerFactory.StartAsync(cancellationToken);
        logger.LogInformation("Informer factory started");

        await informerFactory.WaitForCacheSyncAsync(cancellationToken);
        logger.LogInformation("Cache synced successfully");

        // Finally start the controllers
        foreach (var controller in controllers)
        {
            var task = controller.RunAsync(_stoppingCts.Token);
            _controllerTasks.Add(task);
            logger.LogInformation("Started controller: {ControllerType}", controller.GetType().GetFriendlyName());
        }
    }
}
