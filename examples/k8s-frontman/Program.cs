using k8s.Frontman.Features.Providers;
using k8s.Frontman.Features.Releases;
using k8s.Operator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();

builder.Services.AddOperator(x =>
{
    x.Operator.Name = "k8s-frontman";
    x.Operator.Namespace = "default";
    x.Operator.ContainerRegistry = "ghcr.io";
    x.Operator.ContainerRepository = "pmdevers/k8s-frontman";
    x.Operator.ContainerTag = "v1.0.0";
});

var app = builder.Build();

app.UseResponseCaching();
app.UseResponseCompression();

app.MapProvider();
app.MapRelease();

await app.RunOperatorAsync();

