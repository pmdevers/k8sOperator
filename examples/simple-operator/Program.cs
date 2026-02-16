using k8s.Operator;
using simple_operator.Features.ManageApplication;
using simple_operator.Features.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddOperator(x =>
{
    x.Name = "simple-operator";
    x.Namespace = "default";
    x.Container.Registry = "ghcr.io";
    x.Container.Organization = "myorg";
    x.Container.Image = "simple-operator";
    x.Container.Tag = "v1.0.0";
});

var app = builder.Build();

app.MapTodoItemReconciler();
app.MapMyAppReconciler();

await app.RunOperatorAsync();
