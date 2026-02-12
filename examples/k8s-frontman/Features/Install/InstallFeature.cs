using k8s.Models;
using k8s.Operator.Builders;
using k8s.Operator.Generation;

namespace k8s.Frontman.Features.Install;

public static class InstallFeature
{
    extension(OperatorBuilder builder)
    {
        public OperatorBuilder WithDeployment()
        {
            builder.InstallCommand.Deployment = deployment =>
            {
                deployment.Add(d =>
                {
                    d.Spec.Template.Spec.Containers[0].Ports ??= [];
                    d.Spec.Template.Spec.Containers[0].Ports.Add(new()
                    {
                        ContainerPort = 8080,
                        Name = "http",
                    });
                });
            };
            return builder;
        }

        public OperatorBuilder WithService()
        {
            builder.InstallCommand.AdditionalObjects.Add(
                KubernetesObjectBuilder.CreateMeta<V1Service>()

                    .WithName(builder.Operator.Name)
                    .WithNamespace(builder.Operator.Namespace)
                    .WithLabel("operator", builder.Operator.Name)
                    .WithSpec(s =>
                    {
                        s.AddSelector("operator", builder.Operator.Name);
                        s.AddPort(p =>
                        {
                            p.WithName("http");
                            p.WithPort(8080);
                            p.WithTargetPort(8080);
                        });
                    }).Build()
            );

            return builder;
        }
    }
}
