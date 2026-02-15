using k8s.Models;
using Simplicity.Operator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpperator();

var app = builder.Build();

//app.AddInformer<V1Pod>(informer =>
//{
//    informer.OnAdd += (pod, ct) =>
//    {
//        Console.WriteLine($"Pod added: {pod.Name()}");
//    };
//    informer.OnUpdate += (oldPod, newPod, ct) =>
//    {
//        Console.WriteLine($"Pod updated: {newPod.Name()}");
//    };
//    informer.OnDelete += (pod, ct) =>
//    {
//        Console.WriteLine($"Pod deleted: {pod.Name()}");
//    };
//});

app.AddReconciler<V1Pod>(async (ctx) =>
{
    ctx.Logger.LogInformation("Reconciling pod: {Name}", ctx.Resource.Name());
    await Task.Delay(1000, ctx.CancellationToken); // Simulate some work
    ctx.Logger.LogInformation("Finished reconciling pod: {Name}", ctx.Resource.Name());


    await ctx.Queue.Requeue(ctx.Resource, TimeSpan.FromSeconds(30)); // Requeue the resource for reconciliation after 30 seconds

});

await app.RunAsync();
