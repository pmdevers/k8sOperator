using k8s.Operator.Models;

namespace k8s.Operator.Informer;

public interface IInformer<TResource>
{
    IAsyncEnumerable<WatchEvent<TResource>> Events { get; }

    IReadOnlyList<TResource> List();

    TResource? Get(string name, string? ns = null);

    bool HasSynced { get; }
}
