using k8s.Operator;
using k8s.Operator.Builders;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace k8s.Operator;

public class OperatorContext(IServiceProvider serviceProvider)
{
    private readonly IInformerFactory _informerFactory = serviceProvider.GetRequiredService<IInformerFactory>();
    private readonly IKubernetes _kubernetes = serviceProvider.GetRequiredService<IKubernetes>();
    private readonly IWorkQueue<ResourceKey> _queue = serviceProvider.GetRequiredService<IWorkQueue<ResourceKey>>();

    public IServiceProvider Services { get; } = serviceProvider;
    public required CustomResource? Resource { get; init; }
    public required ResourceKey ResourceKey { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public IKubernetes Kubernetes => _kubernetes;
    public IWorkQueue<ResourceKey> Queue => _queue;

    public IInformer<TResource> GetInformer<TResource>()
        where TResource : CustomResource
    {
        return _informerFactory.GetInformer<TResource>();
    }

    public ResourceUpdateBuilder<TResource> Update<TResource>()
        where TResource : CustomResource
    {
        return new ResourceUpdateBuilder<TResource>(Kubernetes, (TResource)Resource!);
    }
}
