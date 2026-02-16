using k8s.Frontman;
using k8s.Frontman.Features.Install;
using k8s.Frontman.Features.Providers;
using k8s.Frontman.Features.Releases;
using k8s.Operator;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization with source generation
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();

builder.Services.AddOperator(x =>
{
    x.WithDeployment();
    x.WithService();

    x.Install.Resources.Add(typeof(V1Provider));
    x.Install.Resources.Add(typeof(V1Release));
});

var app = builder.Build();

app.UseResponseCaching();
app.UseResponseCompression();

app.MapProvider();
app.MapRelease();

await app.RunOperatorAsync();

