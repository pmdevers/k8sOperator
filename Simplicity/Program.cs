using k8s.Models;
using Simplicity.Operator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpperator();

var app = builder.Build();

app.AddInformer<V1Pod>(informer =>
{
    informer.OnAdd += (pod, ct) =>
    {
        Console.WriteLine($"Pod added: {pod.Name()}");
    };
    informer.OnUpdate += (oldPod, newPod, ct) =>
    {
        Console.WriteLine($"Pod updated: {newPod.Name()}");
    };
    informer.OnDelete += (pod, ct) =>
    {
        Console.WriteLine($"Pod deleted: {pod.Name()}");
    };
});

app.AddReconciler<V1Pod>(async (ctx) =>
{
    Console.WriteLine($"Reconciling pod: {ctx.Item.Name()}");
    await Task.Delay(1000, ctx.CancellationToken); // Simulate some work
    Console.WriteLine($"Finished reconciling pod: {ctx.Item.Name()}");
});

await app.RunAsync();
