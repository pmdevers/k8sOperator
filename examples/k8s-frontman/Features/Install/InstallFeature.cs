using k8s.Models;
using k8s.Operator.Configuration;
using k8s.Operator.Generation;

namespace k8s.Frontman.Features.Install;

public static class InstallFeature
{
    extension(OperatorConfiguration builder)
    {
        public OperatorConfiguration WithDeployment()
        {
            builder.Install.ConfigureDeployment = d =>
            {
                d.Spec.Template.Spec.Containers[0].Ports ??= [];
                d.Spec.Template.Spec.Containers[0].Ports.Add(new()
                {
                    ContainerPort = 8080,
                    Name = "http",
                });
            };
            return builder;
        }

        public OperatorConfiguration WithService()
        {
            builder.Install.AdditionalObjects.Add(
                KubernetesObjectBuilder.Create<V1Service>()
                    .WithName(builder.Name)
                    .WithLabel("operator", builder.Name)
                    .WithSpec(s =>
                    {
                        s.WithSelector("operator", builder.Name);
                        s.WithPort("web", 8080, 8080);
                    }).Build()
            );

            return builder;
        }
    }
}
