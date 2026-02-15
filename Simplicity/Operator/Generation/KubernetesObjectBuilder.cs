using k8s;

namespace Simplicity.Operator.Generation;

public class KubernetesObjectBuilder
{
    public static IObjectBuilder<TResource> Create<TResource>()
        where TResource : IKubernetesObject, new()
    {
        return Create(new TResource());
    }
    public static IObjectBuilder<TResource> Create<TResource>(TResource instance)
        where TResource : IKubernetesObject, new()
    {
        var builder = ObjectBuilder.Create(instance);

        builder.Add(r =>
        {
            r.Initialize();
        });

        return builder;
    }
}
