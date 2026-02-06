using k8s.Operator;
using simple_operator.Features.ManageApplication;
using simple_operator.Features.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddOperator(x =>
{
    x.Operator.Name = "simple-operator";
    x.Operator.Namespace = "default";
    x.Operator.ContainerRegistry = "ghcr.io";
    x.Operator.ContainerRepository = "myorg/simple-operator";
    x.Operator.ContainerTag = "v1.0.0";
});

var app = builder.Build();

app.MapTodoItemReconciler();
app.MapMyAppReconciler();

await app.RunOperatorAsync();
