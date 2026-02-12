using k8s.Operator;
using k8s.Operator.Builders;
using k8s.Operator.Generation.Attributes;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace k8s.Operator.Controller;

public partial class OperatorController<TResource>(
    IServiceProvider serviceProvider,
    IInformerFactory informerFactory,
    IWorkQueue<ResourceKey> queue,
    ReconcileDelegate reconcile,
    IReadOnlyList<object> metadata,
    ILogger logger) : IController
    where TResource : CustomResource
{
    private readonly IInformer<TResource> _informer = informerFactory.GetInformer<TResource>();
    private readonly ConcurrentDictionary<ResourceKey, ResourceSchedule> _schedules = new();

    public IReadOnlyList<object> Metadata { get; } = metadata;
    public Type ResourceType { get; } = typeof(TResource);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => PumpEventsAsync(cancellationToken), cancellationToken);
        _ = Task.Run(() => SchedulerLoop(cancellationToken), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var key = await queue.DequeueAsync(cancellationToken);
            try
            {
                logger.LogInformation("Dequeued resource {ResourceKey} for reconciliation", key);

                using var scope = serviceProvider.CreateAsyncScope();

                var resource = _informer.Get(name: key.Name, ns: key.Namespace);

                var context = new OperatorContext(scope.ServiceProvider)
                {
                    ResourceKey = key,
                    Resource = resource,
                    CancellationToken = cancellationToken,
                };

                if (reconcile != null)
                {
                    await reconcile.Invoke(context);
                }

                // Update last reconciliation time and schedule next reconciliation
                if (resource != null)
                {
                    var interval = GetResyncInterval(resource);
                    _schedules[key] = new ResourceSchedule
                    {
                        LastReconciled = DateTime.UtcNow,
                        Interval = interval,
                        NextReconciliation = DateTime.UtcNow + interval
                    };

                    logger.LogDebug("Scheduled next reconciliation for {ResourceKey} in {Interval}",
                        key, interval);
                }

                logger.LogInformation("Finished reconciling resource {ResourceKey}", key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reconciling resource {ResourceKey}", key);
                await queue.Requeue(key, TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task PumpEventsAsync(CancellationToken cancellationToken)
    {
        await foreach (var evt in _informer.Events.WithCancellation(cancellationToken))
        {
            var key = new ResourceKey(
                Name: evt.Object.Metadata.Name,
                Namespace: evt.Object.Metadata.NamespaceProperty);

            logger.LogDebug("Enqueuing resource {ResourceKey} due to event {EventType}", key, evt.Type);
            await queue.EnqueueAsync(key, cancellationToken);

            // Handle deletions - remove from schedule
            if (evt.Type == WatchEventType.Deleted)
            {
                _schedules.TryRemove(key, out _);
            }
        }
    }

    private async Task SchedulerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var resources = _informer.List();

                foreach (var resource in resources)
                {
                    var key = new ResourceKey(
                        Name: resource.Metadata.Name,
                        Namespace: resource.Metadata.NamespaceProperty);

                    if (_schedules.TryGetValue(key, out var schedule))
                    {
                        // Check if it's time to reconcile
                        if (now >= schedule.NextReconciliation)
                        {
                            logger.LogInformation("Scheduling periodic reconciliation for {ResourceKey}", key);
                            await queue.EnqueueAsync(key, cancellationToken);
                        }
                    }
                    else
                    {
                        // New resource or no schedule yet - enqueue it
                        logger.LogInformation("No schedule found for {ResourceKey}, enqueueing", key);
                        await queue.EnqueueAsync(key, cancellationToken);
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Error in scheduler loop");
            }

            // Check every 10 seconds for resources that need reconciliation
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private static TimeSpan GetResyncInterval(TResource resource)
    {
        // Use reflection to get the Interval property from the Spec
        var specProperty = typeof(TResource).GetProperty("Spec");
        if (specProperty == null)
        {
            return TimeSpan.FromMinutes(5); // Default fallback
        }

        var spec = specProperty.GetValue(resource);
        if (spec == null)
        {
            return TimeSpan.FromMinutes(5);
        }

        var intervalProperty = specProperty.PropertyType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => x.GetCustomAttribute<ResyncIntervalAttribute>() != null);

        if (intervalProperty == null)
        {
            return TimeSpan.FromMinutes(5);
        }

        var intervalValue = intervalProperty.GetValue(spec) as string;
        if (string.IsNullOrEmpty(intervalValue))
        {
            return TimeSpan.FromMinutes(5);
        }

        return ParseDuration(intervalValue);
    }

    private static TimeSpan ParseDuration(string duration)
    {
        // Parse durations like "5m", "30s", "1h", "2h30m"
        var regex = Resync();
        var matches = regex.Matches(duration);

        if (matches.Count == 0)
        {
            return TimeSpan.FromMinutes(5); // Default fallback
        }

        var totalMilliseconds = 0.0;

        foreach (Match match in matches)
        {
            var value = double.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;

            totalMilliseconds += unit switch
            {
                "ms" => value,
                "s" => value * 1000,
                "m" => value * 60 * 1000,
                "h" => value * 60 * 60 * 1000,
                _ => 0
            };
        }

        return TimeSpan.FromMilliseconds(totalMilliseconds);
    }

    private sealed class ResourceSchedule
    {
        public DateTime LastReconciled { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime NextReconciliation { get; set; }
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)(ms|s|m|h)")]
    private static partial Regex Resync();
}
