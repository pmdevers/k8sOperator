using k8s.Models;
using k8s.Operator.Controller;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace k8s.Operator.Builders;

public delegate Task ReconcileDelegate(OperatorContext context);

public class OperatorContext
{
    public IServiceProvider RequestServices { get; set; }
    public IKubernetesObject<V1ObjectMeta>? Resource { get; set; }
    public ResourceKey ResourceKey { get; set; }
}

public class ControllerBuilder(IServiceProvider serviceProvider, Type resourceType)
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public Type ResourceType { get; } = resourceType;

    public ReconcileDelegate? Handler { get; private set; }

    public List<object> MetaData { get; } = [];

    public ControllerBuilder WithHandler(ReconcileDelegate handler)
    {
        Handler = handler;
        return this;
    }

    public IController Build()
    {
        ArgumentNullException.ThrowIfNull(Handler);
        var controllerType = typeof(InformerController<>).MakeGenericType(ResourceType);
        var factory = ServiceProvider.GetRequiredService<IInformerFactory>();
        var queue = ServiceProvider.GetRequiredService<IWorkQueue<ResourceKey>>();
        var logger = ServiceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(controllerType));

        return Activator.CreateInstance(
            controllerType,
            ServiceProvider,
            factory,
            queue,
            Handler,
            logger
        ) as IController ?? throw new InvalidOperationException($"Could not create controller of type {controllerType.FullName}");
    }
}

public class DelegateFactoryOptions
{
    public IServiceProvider? ServiceProvider { get; set; }
    public ControllerBuilder? Builder { get; set; }
}

public class DelegateFactoryContext
{
    public required IServiceProvider ServiceProvider { get; init; }
    public required IServiceProviderIsService? ServiceProviderIsService { get; init; }
    public required ControllerBuilder Builder { get; init; }
    public Delegate? Handler { get; set; }
    public Dictionary<string, string> TrackedParameters { get; } = [];
    public List<ParameterInfo> Parameters { get; set; } = [];

    public Expression[]? ArgumentExpressions { get; set; }
    public Type[]? ArgumentTypes { get; set; }
    public Expression[]? BoxedArgs { get; set; }

    public bool HasInferredBody { get; set; }
    public Expression? MethodCall { get; set; }

    public Type ResourceType { get; set; } = typeof(CustomResource);

    public static DelegateFactoryContext Create(Delegate? handler, DelegateFactoryOptions? options)
    {
        var serviceProvider = options?.ServiceProvider ?? EmptyServiceProvider.Instance;
        var builder = options?.Builder ?? new RfdControllerBuilder(serviceProvider, typeof(CustomResource));

        return new DelegateFactoryContext()
        {
            ServiceProvider = serviceProvider,
            ServiceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>(),
            Builder = builder,
            Handler = handler,
        };
    }
}

public static class DelegateFactory
{
    public static ReconcileDelegate Create(Delegate handler, DelegateFactoryOptions? options = null)
    {
        var targetExpression = handler.Target switch
        {
            object => Expression.Constant(handler.Target, handler.Target.GetType()),
            null => null,
        };

        var factoryContext = DelegateFactoryContext.Create(handler, options);
        var targetableDelegate = CreateTargetableDelegate(handler.Method, targetExpression, factoryContext);

        var finalDelegate = targetableDelegate switch
        {
            null => (ReconcileDelegate)handler,
            _ => context => targetableDelegate(handler.Target, context)
        };

        return finalDelegate;
    }

    private static Func<object?, OperatorContext, Task>? CreateTargetableDelegate(
        MethodInfo method,
        ConstantExpression targetExpression,
        DelegateFactoryContext factoryContext)
    {
        factoryContext.ArgumentExpressions ??= CreateArguments(method.GetParameters(), factoryContext);
        factoryContext.MethodCall = CreateMethodCall(method, targetExpression, factoryContext);

        Func<object?, OperatorContext, Task> continuation;

        if (factoryContext.MethodCall.Type == typeof(void))
        {
            var block = Expression.Block(
                factoryContext.MethodCall,
                Expression.Constant(Task.CompletedTask)
            );
            continuation = Expression.Lambda<Func<object?, OperatorContext, Task>>(
                block,
                TargetExpr,
                ContextExpr
            ).Compile();
        }
        else
        {
            continuation = Expression.Lambda<Func<object?, OperatorContext, Task>>(
                factoryContext.MethodCall,
                TargetExpr,
                ContextExpr
            ).Compile();
        }

        if (factoryContext.Handler is ReconcileDelegate)
        {
            return null;
        }

        return async (target, context) =>
        {
            await continuation(target, context);
        };
    }

    private static Expression CreateMethodCall(MethodInfo method, ConstantExpression target, DelegateFactoryContext factoryContext)
        => target is null
            ? Expression.Call(method, factoryContext.ArgumentExpressions!)
            : Expression.Call(target, method, factoryContext.ArgumentExpressions!);

    private static Expression[]? CreateArguments(ParameterInfo[] parameters, DelegateFactoryContext factoryContext)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return null;
        }

        var arguments = new Expression[parameters.Length];

        factoryContext.ArgumentTypes = new Type[parameters.Length];
        factoryContext.BoxedArgs = new Expression[parameters.Length];
        factoryContext.Parameters = [.. parameters];

        for (int i = 0; i < parameters.Length; i++)
        {
            arguments[i] = CreateArgument(parameters[i], factoryContext);

            if (factoryContext.HasInferredBody)
            {
                throw new InvalidOperationException("The delegate has an inferred body, which is not supported in this context.");
            }

            factoryContext.ArgumentTypes[i] = arguments[i].Type;
            factoryContext.BoxedArgs[i] = Expression.Convert(arguments[i], typeof(object));
        }

        return arguments;
    }

    private static Expression CreateArgument(ParameterInfo parameter, DelegateFactoryContext factoryContext)
    {
        if (parameter.Name is null)
            throw new InvalidOperationException("Parameter name cannot be null.");

        if (parameter.ParameterType.IsByRef)
        {
            var attribute = "ref";

            if (parameter.Attributes.HasFlag(ParameterAttributes.In))
            {
                attribute = "in";
            }
            else if (parameter.Attributes.HasFlag(ParameterAttributes.Out))
            {
                attribute = "out";
            }

            throw new NotSupportedException($"Parameter '{parameter.Name}' has '{attribute}' modifier, which is not supported.");
        }

        var attributes = parameter.GetCustomAttributes();

        if (parameter.ParameterType == typeof(OperatorContext))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, "Context (Inferred)");
            return ContextExpr;
        }

        if (parameter.ParameterType == factoryContext.Builder.ResourceType)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, "Resource (Inferred)");
            var resourceExpr = Expression.Property(ContextExpr, nameof(OperatorContext.Resource));
            return Expression.Convert(resourceExpr, parameter.ParameterType);
        }

        if (parameter.ParameterType == typeof(ResourceKey))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, "ResourceKey (Inferred)");
            var resourceExpr = Expression.Property(ContextExpr, nameof(OperatorContext.ResourceKey));
            return Expression.Convert(resourceExpr, parameter.ParameterType);
        }

        if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService &&
            serviceProviderIsService.IsService(parameter.ParameterType))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, "Services (Inferred)");
            return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
        }

        factoryContext.HasInferredBody = true;
        return Expression.Empty();
    }

    private static readonly ParameterExpression ContextExpr = Expression.Parameter(typeof(OperatorContext), "context");
    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");
    private static readonly MemberExpression RequestServicesExpr = Expression.Property(ContextExpr,
        typeof(OperatorContext).GetProperty(nameof(OperatorContext.RequestServices))!);

    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions)
        .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService),
        BindingFlags.Public | BindingFlags.Static, [typeof(IServiceProvider)])!;
}

internal sealed class EmptyServiceProvider : IServiceProvider
{
    public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
    public object? GetService(Type serviceType) => null;
}

internal class RfdControllerBuilder(IServiceProvider serviceProvider, Type resourceType) : ControllerBuilder(serviceProvider, resourceType)
{
}
