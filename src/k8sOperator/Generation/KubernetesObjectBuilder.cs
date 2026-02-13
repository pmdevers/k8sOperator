using k8s.Models;

namespace k8s.Operator.Generation;

/// <summary>
/// Describes a Generic Kubernetes Resource Builder
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IObjectBuilder<out T>
{
    /// <summary>
    /// Adds an action to the builder.
    /// </summary>
    /// <param name="action"></param>
    IObjectBuilder<T> Add(Action<T> action);

    /// <summary>
    /// Creates and returns an instance of type <typeparamref name="T"/> based on the current configuration.
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/> constructed according to the configured settings.</returns>
    T Build();
}


public static class ObjectBuilder
{
    public static ObjectBuilder<TResource> Create<TResource>()
        where TResource : new()
    {
        return Create<TResource>(new());
    }

    public static ObjectBuilder<TResource> Create<TResource>(TResource instance)
    {
        return new ObjectBuilder<TResource>(instance);
    }
}

public class ObjectBuilder<T> : IObjectBuilder<T>
{
    internal ObjectBuilder(T instance)
    {
        _instance = instance;
    }

    private readonly List<Action<T>> _actions = [];
    private readonly T _instance;

    public IObjectBuilder<T> Add(Action<T> action)
    {
        _actions.Add(action);
        return this;
    }

    public virtual T Build()
    {
        var o = _instance;

        foreach (var action in _actions)
        {
            action(o);
        }

        return o;
    }

    public ObjectBuilder<T> For(T instance)
    {
        var clone = new ObjectBuilder<T>(instance);
        clone._actions.AddRange(_actions);
        return clone;
    }
}

public static class KubernetesObjectBuilder
{
    /// <summary>
    /// Creates a new Kubernetes object builder for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the Kubernetes object.</typeparam>
    /// <returns>A new instance of <see cref="IObjectBuilder{T}"/>.</returns>
    public static IObjectBuilder<T> Create<T>()
        where T : IKubernetesObject, new()
    {
        return Create(new T());
    }

    public static IObjectBuilder<T> Create<T>(T instance)
        where T : IKubernetesObject
    {
        return new ObjectBuilder<T>(instance).Add(x => x.Initialize());
    }

    public static IObjectBuilder<T> CreateMeta<T>()
        where T : IMetadata<V1ObjectMeta>, new()
    {
        return CreateMeta(new T());
    }

    public static IObjectBuilder<T> CreateMeta<T>(T instance)
        where T : IMetadata<V1ObjectMeta>
    {
        return new ObjectBuilder<T>(instance)
            .Add(x =>
            {
                if (x is IKubernetesObject o)
                {
                    o.Initialize();
                }
            })
            .Add(x => x.Metadata = new());
    }
}
