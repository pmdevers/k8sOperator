using k8s.Frontman.Features.Providers;
using k8s.Frontman.Features.Releases;
using k8s.Models;
using k8s.Operator;
using k8s.Operator.Generation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();

builder.Services.AddOperator(x =>
{
    x.Operator.Name = "k8s-frontman";
    x.Operator.Namespace = "default";
    x.Operator.ContainerRegistry = "ghcr.io";
    x.Operator.ContainerRepository = "pmdevers/k8s-frontman";
    x.Operator.UpdateUrl = "https://api.github.com/repos/pmdevers/k8s-frontman/releases/latest";

    x.InstallCommand.Deployment = deployment =>
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

    x.InstallCommand.AdditionalObjects.Add(
        KubernetesObjectBuilder.CreateMeta<V1Service>()

            .WithName(x.Operator.Name)
            .WithNamespace(x.Operator.Namespace)
            .WithLabel("operator", x.Operator.Name)
            .WithSpec(s =>
            {
                s.AddSelector("operator", x.Operator.Name);
                s.AddPort(p =>
                {
                    p.WithName("http");
                    p.WithPort(8080);
                    p.WithTargetPort(8080);
                });
            }).Build()
    );
});

var app = builder.Build();

app.UseResponseCaching();
app.UseResponseCompression();

app.MapProvider();
app.MapRelease();

await app.RunOperatorAsync();

