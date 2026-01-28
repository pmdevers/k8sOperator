using k8s.Operator.Builders;
using k8s.Operator.Controller;
using k8s.Operator.Models;

namespace k8s.Operator;

public class ControllerDatasource(IServiceProvider serviceProvider)
{
    private List<ResourceEntries> _resources = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConventionBuilder<ControllerBuilder> AddResource<TResource>(Delegate reconcile)
        where TResource : CustomResource
    {
        var conventions = new List<Action<ControllerBuilder>>();
        _resources.Add(new()
        {
            ResourceType = typeof(TResource),
            Handler = reconcile,
            Conventions = conventions
        });

        return new ConventionBuilder<ControllerBuilder>(conventions);
    }

    public IEnumerable<IController> GetControllers()
    {
        foreach (var resource in _resources)
        {
            var builder = new ControllerBuilder(ServiceProvider, resource.ResourceType);

            foreach (var conventions in resource.Conventions)
            {
                conventions(builder);
            }

            var del = DelegateFactory.Create(resource.Handler, new DelegateFactoryOptions()
            {
                ServiceProvider = ServiceProvider,
                Builder = builder
            });

            builder.WithHandler(del);

            yield return builder.Build();
        }


    }

    private struct ResourceEntries
    {
        public Type ResourceType { get; set; }
        public Delegate Handler { get; set; }
        public required List<Action<ControllerBuilder>> Conventions { get; set; }
    }
}
