namespace k8s.Operator.Generation;

public interface IObjectBuilder<out T>
{
    IObjectBuilder<T> Add(Action<T> action);
    T Build();
}

public static class ObjectBuilder
{
    public static IObjectBuilder<T> Create<T>()
        where T : new()
    {
        return Create<T>(new());
    }
    public static IObjectBuilder<T> Create<T>(T instance)
    {
        return new ObjectBuilder<T>(instance);
    }
}

public class ObjectBuilder<T>(T instance) : IObjectBuilder<T>
{
    private readonly List<Action<T>> _actions = [];

    public IObjectBuilder<T> Add(Action<T> action)
    {
        _actions.Add(action);
        return this;
    }

    public T Build()
    {
        foreach (var action in _actions)
        {
            action(instance);
        }
        return instance;
    }
}
