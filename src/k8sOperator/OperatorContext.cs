using k8s.Operator;
using k8s.Operator.Builders;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using Microsoft.Extensions.DependencyInjection;

namespace k8s.Operator;

public class OperatorContext(IServiceProvider serviceProvider)
{
    private readonly IInformerFactory _informerFactory = serviceProvider.GetRequiredService<IInformerFactory>();
    private readonly IKubernetes _kubernetes = serviceProvider.GetRequiredService<IKubernetes>();

    public IServiceProvider Services { get; } = serviceProvider;
    public required CustomResource? Resource { get; init; }
    public required ResourceKey ResourceKey { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public IKubernetes Kubernetes => _kubernetes;

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
