using k8s.Operator;
using simple_operator.Features.Applications;
using simple_operator.Features.Todos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddOperator(x =>
{
    x.Name = "simple-operator";
    x.Namespace = "default";
});

var app = builder.Build();

app.MapTodoItemReconciler();
app.MapMyAppReconciler();

await app.RunOperatorAsync();
