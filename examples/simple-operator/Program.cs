using k8s.Operator;
using simple_operator.Features.ManageApplication;
using simple_operator.Features.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddOperator(x =>
{
    x.OperatorConfiguration.OperatorName = "simple-operator";
    x.OperatorConfiguration.Namespace = "default";
    x.OperatorConfiguration.ContainerRegistry = "ghcr.io";
    x.OperatorConfiguration.ContainerRepository = "myorg/simple-operator";
    x.OperatorConfiguration.ContainerTag = "v1.0.0";
});

var app = builder.Build();

app.MapTodoItemReconciler();
app.MapMyAppReconciler();

await app.RunOperatorAsync();
