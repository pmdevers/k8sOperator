using k8s.Operator.Controller;
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
    private Task? _backgroundTask;


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start the operator logic in a background task
        _backgroundTask = Task.Run(async () => await RunOperatorLoopAsync(_stoppingCts.Token), cancellationToken);

        logger.LogInformation("Operator Service started in background");

        // Return immediately so the web API can start
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Operator Service...");

        if (_stoppingCts != null)
            await _stoppingCts.CancelAsync();

        // Wait for the background task to complete
        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        if (_controllerTasks.Count > 0)
        {
            await Task.WhenAll(_controllerTasks);
        }

        _stoppingCts?.Dispose();

        logger.LogInformation("Operator Service stopped");
    }

    private async Task RunOperatorLoopAsync(CancellationToken cancellationToken)
    {
        // Build all controllers first (this registers informers)
        var controllers = controllerDatasource.GetControllers().ToList();
        logger.LogInformation("Built {Count} controller(s)", controllers.Count);

        // Start informers immediately, regardless of leadership status
        await informerFactory.StartAsync(cancellationToken);
        logger.LogInformation("Informer factory started");

        await informerFactory.WaitForCacheSyncAsync(cancellationToken);
        logger.LogInformation("Cache synced successfully");

        _ = Task.Run(() =>
            leaderElectionService.StartAsync(cancellationToken), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Waiting for leadership...");

                await leaderElectionService.WaitForLeadershipAsync(cancellationToken);

                logger.LogInformation("Leadership acquired");

                using var watcherCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                await RunControllersAsync(controllers, watcherCts.Token);

                await leaderElectionService.WaitForLeadershipLostAsync(cancellationToken);

                logger.LogInformation("Leadership lost, stopping controllers...");

                // Leadership lost or stopping, cancel watchers
                await watcherCts.CancelAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in operator loop");
                // Wait a bit before retrying to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task RunControllersAsync(List<IController> controllers, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting controllers...");

        // Start the controllers (informers are already running)
        _controllerTasks.Clear();
        foreach (var controller in controllers)
        {
            var task = controller.RunAsync(cancellationToken);
            _controllerTasks.Add(task);
            logger.LogInformation("Started controller: {ControllerType}", controller.GetType().GetFriendlyName());
        }

        // Wait for cancellation (leadership loss or shutdown)
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when leadership is lost or stopping
        }
    }
}
