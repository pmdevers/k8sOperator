using k8s.Models;

namespace k8s.Operator.Generation;

/// <summary>
/// Provides extension methods for configuring Kubernetes objects with metadata.
/// </summary>
public static class KubernetesObjectBuilderExtentions
{
    extension<T>(IObjectBuilder<T> builder)
        where T : IMetadata<V1ObjectMeta>
    {
        public IObjectBuilder<T> WithName(string name)
        {
            builder.Add(x =>
            {
                x.Metadata.Name = name;
            });
            return builder;
        }
        public IObjectBuilder<T> WithNamespace(string ns)
        {
            builder.Add(x =>
            {
                x.Metadata.SetNamespace(ns);
            });
            return builder;
        }
        public IObjectBuilder<T> WithAnnotation(string key, string value)
        {
            builder.Add(x =>
            {
                x.Metadata.Annotations ??= new Dictionary<string, string>();
                x.Metadata.Annotations.Add(key, value);
            });
            return builder;
        }
        public IObjectBuilder<T> WithLabel(string key, string value)
        {
            builder.Add(x =>
            {
                x.Metadata.Labels ??= new Dictionary<string, string>();
                x.Metadata.Labels.Add(key, value);
            });
            return builder;
        }
        public IObjectBuilder<T> WithFinalizer(string finalizer)
        {
            builder.Add(x =>
            {
                x.Metadata.Finalizers ??= [];
                if (!x.Metadata.Finalizers.Contains(finalizer))
                {
                    x.Metadata.Finalizers.Add(finalizer);
                }
            });
            return builder;
        }
        public IObjectBuilder<T> RemoveFinalizer(string finalizer)
        {
            builder.Add(x =>
            {
                x.Metadata.Finalizers?.Remove(finalizer);
            });
            return builder;
        }

    }
}
