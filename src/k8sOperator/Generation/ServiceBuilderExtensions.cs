using k8s.Models;

namespace k8s.Operator.Generation;

public static class ServiceBuilderExtensions
{
    extension(IObjectBuilder<V1Service> builder)
    {
        public IObjectBuilder<V1Service> WithSpec(Action<IObjectBuilder<V1ServiceSpec>> configure)
        {
            var b = ObjectBuilder.Create<V1ServiceSpec>();
            configure(b);
            builder.Add(x => x.Spec = b.Build());
            return builder;
        }
    }

    extension(IObjectBuilder<V1ServiceSpec> builder)
    {
        public IObjectBuilder<V1ServiceSpec> AddSelector(string name, string value)
        {
            builder.Add(x =>
            {
                x.Selector ??= new Dictionary<string, string>();
                x.Selector.Add(name, value);
            });
            return builder;
        }

        public IObjectBuilder<V1ServiceSpec> AddPort(Action<IObjectBuilder<V1ServicePort>> configure)
        {
            var b = ObjectBuilder.Create<V1ServicePort>();
            configure(b);
            builder.Add(x =>
            {
                x.Ports ??= [];
                x.Ports.Add(b.Build());
            });
            return builder;
        }
    }

    extension(IObjectBuilder<V1ServicePort> builder)
    {
        public IObjectBuilder<V1ServicePort> WithName(string name)
        {
            builder.Add(x => x.Name = name);
            return builder;
        }
        public IObjectBuilder<V1ServicePort> WithPort(int port)
        {
            builder.Add(x => x.Port = port);
            return builder;
        }
        public IObjectBuilder<V1ServicePort> WithTargetPort(int targetPort)
        {
            builder.Add(x => x.TargetPort = targetPort);
            return builder;
        }
    }
}
