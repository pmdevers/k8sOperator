using k8s;
using k8s.Models;
using System.Reflection;

namespace k8sOperator.Tests.TestKube;

public class TestKubeApiBuilder(IEndpointRouteBuilder builder)
{
    private readonly IEndpointRouteBuilder _builder = builder;

    public CustomObjectsImpl CustomObjects => new(_builder);

    public class CustomObjectsImpl(IEndpointRouteBuilder builder)
    {
        public void WatchListClusterCustomObjectAsync<T>(Watcher<T>.WatchEvent? watchEvent = null, string ns = "default")
             where T : IKubernetesObject<V1ObjectMeta>, new()
        {
            var attr = typeof(T).GetCustomAttribute<KubernetesEntityAttribute>();
            var group = attr?.Group ?? throw new InvalidOperationException($"KubernetesEntityAttribute.Group not defined on {typeof(T).FullName}");
            var version = attr?.ApiVersion ?? throw new InvalidOperationException($"KubernetesEntityAttribute.Version not defined on {typeof(T).FullName}");
            var plural = attr?.PluralName ?? throw new InvalidOperationException($"KubernetesEntityAttribute.Plural not defined on {typeof(T).FullName}");

            builder.MapGet($"/apis/{group}/{version}/namespaces/{ns}/{plural}", async context =>
            {
                var isWatch = context.Request.Query["watch"].ToString() == "true";

                if (!isWatch || watchEvent is null)
                {
                    var j = KubernetesJson.Serialize(new T());
                    await context.Response.WriteAsync(j);
                    return;
                }

                // For watch requests, send the event as newline-delimited JSON
                var json = KubernetesJson.Serialize(watchEvent);
                await context.Response.WriteAsync(json);
                await context.Response.WriteAsync("\n");
                await context.Response.Body.FlushAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(true);
            });
        }

        public void ReplaceNamespacedCustomObjectAsync<T>(string ns = "default", Action<T?>? resource = null)
            where T : IKubernetesObject<V1ObjectMeta>, new()
        {
            var attr = typeof(T).GetCustomAttribute<KubernetesEntityAttribute>();
            var group = attr?.Group ?? throw new InvalidOperationException($"KubernetesEntityAttribute.Group not defined on {typeof(T).FullName}");
            var version = attr?.ApiVersion ?? throw new InvalidOperationException($"KubernetesEntityAttribute.Version not defined on {typeof(T).FullName}");
            var plural = attr?.PluralName ?? throw new InvalidOperationException($"KubernetesEntityAttribute.Plural not defined on {typeof(T).FullName}");

            builder.MapPut($"/apis/{group}/{version}/namespaces/{ns}/{plural}/{{name}}", async context =>
            {
                // Mock replacing a custom resource
                var requestBody = KubernetesJson.Deserialize<T>(context.Request.Body);

                resource?.Invoke(requestBody);

                var jsonResponse = KubernetesJson.Serialize(requestBody);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(jsonResponse);
            });
        }
    }
}
