using k8s.Models;
using Simplicity.Operator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpperator(x =>
{
    x.Name = "Simplicity Operator";
    x.Version = "1.0.0";
});

var app = builder.Build();

app.AddInformer<V1Deployment>();
app.AddReconciler<V1Pod>(async (ctx) =>
{
    ctx.Logger.LogInformation("Reconciling pod: {Name}", ctx.Resource.Name());
    await Task.Delay(1000, ctx.CancellationToken); // Simulate some work
    ctx.Logger.LogInformation("Finished reconciling pod: {Name}", ctx.Resource.Name());

    await ctx.Queue.Requeue(ctx.Resource, TimeSpan.FromSeconds(30)); // Requeue the resource for reconciliation after 30 seconds

});

await app.RunOperatorAsync();
