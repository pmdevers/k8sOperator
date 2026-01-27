using k8s.Operator;
using simple_operator.Test;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddOperator(x =>
{
    x.OperatorConfiguration.OperatorName = "todo-operator";
    x.OperatorConfiguration.Namespace = "default";
    x.OperatorConfiguration.ContainerRegistry = "ghcr.io";
    x.OperatorConfiguration.ContainerRepository = "myorg/todo-operator";
    x.OperatorConfiguration.ContainerTag = "v1.0.0";
});

var app = builder.Build();

app.ReconcilerFor<TodoItem>(TodoItem.ReconcileAsync);

app.Run();
