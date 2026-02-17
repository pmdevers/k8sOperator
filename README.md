# k8sOperator

A modern Kubernetes Operator framework for .NET that makes it easy to build custom controllers and operators.

## Features

- 🎯 **Simple API** - Fluent builder pattern for Kubernetes resources
- 🔄 **Reconciliation** - Built-in controller pattern with informers and caching
- 📦 **Custom Resources** - Full CRD support with type-safe definitions
- 🗳️ **Leader Election** - High availability out of the box
- 🚀 **AOT Ready** - Optimized for trimmed, single-file deployments
- 🛠️ **CLI Included** - Built-in commands for install, create, and version
- 📊 **Source Generators** - Automatic builder extensions for your resources

## Installation

```bash
dotnet add package k8sOperator
```

## Quick Start

### 1. Define Your Custom Resource

```csharp
[KubernetesEntity(Group = "demo.k8s.io", ApiVersion = "v1", Kind = "MyApp")]
public class MyApp : CustomResource<MyAppSpec, MyAppStatus>
{
}

public class MyAppSpec
{
    public string Image { get; set; }
    public int Replicas { get; set; }
}

public class MyAppStatus
{
    public string Phase { get; set; }
}
```

### 2. Create a Reconciler

```csharp
public class MyAppReconciler : Reconciler<MyApp>
{
    public override async Task<ReconcileResult> ReconcileAsync(
        ReconcileContext<MyApp> context,
        CancellationToken cancellationToken)
    {
        var app = context.Resource;

        // Your reconciliation logic
        // Create/update deployments, services, etc.

        context.Update(x => {
            x.WithStatus(x =>
            {
                x.State = "completed";
                x.CompletedAt = DateTime.UtcNow;
                x.Message = "Todo item has been completed.";
                x.ReconciliationCount++;
            });
        });
    }
}
```

### 3. Register and Run

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOperator(options =>
{
    options.Name = "my-operator";
    options.Namespace = "default";
});

var app = builder.Build();

appapp.AddReconciler<MyApp>(MyOperator.ReconcileAsync);

await app.RunOperatorAsync(args);
```

## Building Resources

Use the fluent builder API:

```csharp
var deployment = ObjectBuilder.Create<V1Deployment>()
    .WithName("my-app")
    .WithNamespace("default")
    .WithLabel("app", "my-app")
    .WithSpec(spec => spec
        .WithReplicas(3)
        .WithTemplate(template => template
            .WithSpec(podSpec => podSpec
                .WithContainer(c => c
                    .WithImage("nginx:latest")
                    .WithPort(80)))))
    .Build();
```

## CLI Commands

```bash
# Show version
myoperator version

# Install CRDs
myoperator install

# Create resource
myoperator create myapp --name demo

# Show help
myoperator help
```

## Configuration

Configure via `appsettings.json` or attributes:

```json
{
  "Operator": {
    "Name": "my-operator",
    "Namespace": "default",
  }
}
```

## Links

- [GitHub](https://github.com/pmdevers/k8sOperator)
- [Examples](https://github.com/pmdevers/k8sOperator/tree/main/examples)
- [Documentation](https://github.com/pmdevers/k8sOperator/wiki)

## Requirements

- .NET 10.0 or later
- Kubernetes 1.25+

## License

MIT License

---

Built with ❤️ by [Patrick Evers](https://github.com/pmdevers)
