using k8s.Models;
using k8s.Operator;
using k8s.Operator.Configuration;
using k8s.Operator.Controller;
using k8s.Operator.Generation;
using k8s.Operator.Metadata;

namespace k8s.Operator.Host.Commands;

public class InstallCommandOptions
{
    public Action<IObjectBuilder<V1Deployment>>? Deployment { get; set; } = null;
    public List<IKubernetesObject> AdditionalObjects { get; set; } = [];
}


[OperatorArgument(
    Command = "install",
    Description = "Generates the install manifests",
    Aliases = new[] { "-i", "--install" },
    Order = 1
)]
public class InstallCommand(OperatorConfiguration config, ControllerDatasource datasource, InstallCommandOptions options) : IOperatorCommand
{
    private readonly StringWriter _output = new();

    public async Task RunAsync(string[] args)
    {
        var watchers = datasource.GetControllers().ToList();
        var ns = CreateNamespace(config);
        var clusterrole = CreateClusterRole(config, watchers);
        var clusterrolebinding = CreateClusterRoleBinding(config);

        var deployment = CreateDeployment(config, options.Deployment);

        foreach (var item in watchers)
        {
            var crd = CreateCustomResourceDefinition(item);

            await Write(crd);
        }

        await Write(clusterrole);
        await Write(clusterrolebinding);
        await Write(ns);
        await Write(deployment);

        foreach (var obj in options.AdditionalObjects)
        {
            await Write(obj);
        }

        Console.WriteLine(_output.ToString());
    }

    private async Task Write(IKubernetesObject obj)
    {
        await _output.WriteLineAsync(KubernetesYaml.Serialize(obj));
        await _output.WriteLineAsync("---");
    }

    internal static V1Namespace CreateNamespace(OperatorConfiguration config)
    {
        var nsBuilder = KubernetesObjectBuilder.Create<V1Namespace>();
        nsBuilder.WithName(config.Namespace);

        return nsBuilder.Build();
    }

    internal static V1CustomResourceDefinition CreateCustomResourceDefinition(IController item)
    {
        var group = item.Metadata.OfType<KubernetesEntityAttribute>().First();
        var scope = item.Metadata.OfType<ScopeAttribute>().FirstOrDefault()
            ?? ScopeAttribute.Default;

        var columns = item.Metadata.OfType<AdditionalPrinterColumnAttribute>();

        var crdBuilder = KubernetesObjectBuilder.Create<V1CustomResourceDefinition>();
        crdBuilder
          .WithName($"{group.PluralName}.{group.Group}".ToLower())
          .WithSpec()
              .WithGroup(group.Group)
              .WithNames(
                 kind: group.Kind,
                 kindList: $"{group.Kind}List",
                 plural: group.PluralName.ToLower(),
                 singular: group.Kind.ToLower()
              )
              .WithScope(scope.Scope)
              .WithVersion(
                    group.ApiVersion,
                    schema =>
                    {
                        schema.WithSchemaForType(item.ResourceType);
                        schema.WithServed(true);
                        schema.WithStorage(true);
                        schema.WithSubResources();
                        foreach (var column in columns)
                        {
                            schema.WithAdditionalPrinterColumn(column.Name, column.Type, column.Description, column.Path);
                        }

                    });


        return crdBuilder.Build();
    }

    internal static V1Deployment CreateDeployment(OperatorConfiguration config, Action<IObjectBuilder<V1Deployment>>? customize = null)
    {
        var deployment = KubernetesObjectBuilder.Create<V1Deployment>();

        deployment
            .WithName($"{config.Name}")
            .WithNamespace(config.Namespace)
            .WithLabel("operator", config.Name)
            .WithSpec(x => x
                .WithReplicas(1)
                .WithRevisionHistory(0)
                .WithSelector(matchLabels: x =>
                {
                    x.Add("operator", config.Name);
                })
                .WithTemplate()
                    .WithLabel("operator", config.Name)

                    .WithPod()
                        .WithSecurityContext(b =>
                            b.Add(x =>
                            {
                                x.RunAsNonRoot = true;
                                x.SeccompProfile = new()
                                {
                                    Type = "RuntimeDefault"
                                };
                            }))
                        .WithTerminationGracePeriodSeconds(10)
                        .AddContainer()
                            .AddEnvFromObjectField("NAMESPACE", x => x.FieldPath = "metadata.namespace")
                            .WithSecurityContext(x =>
                            {
                                x.AllowPrivilegeEscalation(false);
                                x.RunAsNonRoot();
                                x.RunAsUser(2024);
                                x.RunAsGroup(2024);
                                x.WithCapabilities(x => x.WithDrop("ALL"));
                            })
                            .WithName(config.Name)
                            .WithImage(config.ContainerImage)
                            .WithResources(
                                limits: x =>
                                {
                                    x.Add("cpu", new ResourceQuantity("100m"));
                                    x.Add("memory", new ResourceQuantity("128Mi"));
                                },
                                requests: x =>
                                {
                                    x.Add("cpu", new ResourceQuantity("100m"));


                                    x.Add("memory", new ResourceQuantity("64Mi"));
                                }
                            ));

        customize?.Invoke(deployment);

        return deployment.Build();
    }

    internal static V1ClusterRoleBinding CreateClusterRoleBinding(OperatorConfiguration config)
    {
        var clusterrolebinding = KubernetesObjectBuilder.Create<V1ClusterRoleBinding>()
            .WithName($"{config.Name}-role-binding")
            .WithRoleRef("rbac.authorization.k8s.io", "ClusterRole", $"{config.Name}-role")
            .WithSubject(kind: "ServiceAccount", name: "default", ns: config.Namespace);

        return clusterrolebinding.Build();
    }

    internal static V1ClusterRole CreateClusterRole(OperatorConfiguration config, IEnumerable<IController> watchers)
    {
        var clusterrole = KubernetesObjectBuilder.Create<V1ClusterRole>()
                    .WithName($"{config.Name}-role");

        clusterrole.AddRule()
            .WithGroups("")
            .WithResources("events")
            .WithVerbs("get", "list", "create", "update");

        clusterrole.AddRule()
            .WithGroups("coordination.k8s.io")
            .WithResources("leases")
            .WithVerbs("create", "update", "get");

        var rules = watchers
            .Select(x => x.Metadata.OfType<KubernetesEntityAttribute>().First())
            .GroupBy(x => x.Group)
            .ToList();

        foreach (var item in rules)
        {
            clusterrole.AddRule()
                    .WithGroups(item.Key)
                    .WithResources([.. item.Select(x => x.PluralName)])
                    .WithVerbs("*");
            clusterrole.AddRule()
                    .WithGroups(item.Key)
                    .WithResources([.. item.Select(x => $"{x.PluralName}/status")])
                    .WithVerbs("get", "update", "patch");
        }

        return clusterrole.Build();
    }

}
