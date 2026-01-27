using k8s.Operator.Models;

namespace simple_operator.Controller;

/// <summary>
/// Example custom resource
/// </summary>
[k8s.Models.KubernetesEntity(Group = "example.io", ApiVersion = "v1", Kind = "MyApp", PluralName = "myapps")]
public class MyApp : CustomResource<MyAppSpec, MyAppStatus>
{
}

public class MyAppSpec
{
    public int Replicas { get; set; }
    public string? Image { get; set; }
}

public class MyAppStatus
{
    public string? Phase { get; set; }
    public int ReadyReplicas { get; set; }
}

///// <summary>
///// Controller implementation using the informer pattern
///// </summary>
//public class MyAppController : InformerController<MyApp>
//{
//    private readonly ILogger<MyAppController> _logger;
//    private readonly IInformer<MyApp> _informer;

//    public MyAppController(
//        IInformerFactory informerFactory,
//        IWorkQueue<ResourceKey> queue,
//        ILogger<MyAppController> logger)
//        : base(informerFactory, queue, logger)
//    {
//        _logger = logger;
//        _informer = informerFactory.GetInformer<MyApp>();
//    }

//    protected override async Task ReconcileAsync(
//        ResourceKey key,
//        MyApp? resource,
//        CancellationToken cancellationToken)
//    {
//        if (resource == null)
//        {
//            _logger.LogInformation("Resource {Name} in namespace {Namespace} was deleted",
//                key.Name, key.Namespace);
//            // Handle deletion
//            return;
//        }

//        _logger.LogInformation("Reconciling MyApp {Name} in namespace {Namespace}",
//            resource.Metadata.Name, resource.Metadata.NamespaceProperty);

//        // Your reconciliation logic here
//        // You can access other resources from the cache
//        var allApps = _informer.List();
//        _logger.LogInformation("Total apps in cache: {Count}", allApps.Count);

//        // Example: Update status
//        if (resource.Status == null)
//        {
//            resource.Status = new MyAppStatus { Phase = "Pending" };
//        }

//        resource.Status.Phase = "Running";
//        resource.Status.ReadyReplicas = resource.Spec?.Replicas ?? 0;

//        _logger.LogInformation("Reconciled MyApp {Name}, replicas: {Replicas}",
//            resource.Metadata.Name, resource.Status.ReadyReplicas);

//        await Task.CompletedTask;
//    }
//}
