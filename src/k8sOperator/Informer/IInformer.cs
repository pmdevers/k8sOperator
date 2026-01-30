using k8s.Operator.Models;

namespace k8s.Operator.Informer;

public interface IInformer<TResource>
    where TResource : CustomResource
{
    IAsyncEnumerable<WatchEvent<TResource>> Events { get; }

    IReadOnlyList<TResource> List();

    TResource? Get(string name, string? ns = null);

    bool HasSynced { get; }
}
