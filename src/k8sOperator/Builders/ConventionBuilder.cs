namespace k8s.Operator.Builders;

/// <summary>
/// Provides a builder for registering conventions to be applied to instances of type <typeparamref name="T"/>.
/// </summary>
public class ConventionBuilder<T>(ICollection<Action<T>> conventions)
{
    /// <summary>
    /// Adds a convention to the builder.
    /// </summary>
    /// <param name="convention">The convention to add.</param>
    /// <returns>The current <see cref="ConventionBuilder{T}"/> instance.</returns>
    public ConventionBuilder<T> Add(Action<T> convention)
    {
        conventions.Add(convention);
        return this;
    }
}

