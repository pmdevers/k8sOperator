using k8s.Models;
using k8s.Operator.Configuration;
using k8s.Operator.Generation;
using k8s.Operator.Informer;
using System.Reflection;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("install", "Install the operator", -1, "-i", "--install")]
public class InstallCommand(OperatorConfiguration config, SharedInformerFactory factory) : IOperatorCommand
{
    private readonly StringWriter _output = new();

    public async Task ExecuteAsync(string[] args)
    {
        var resources = factory.AllTypes().ToList();
        var ns = CreateNamespace(config);
        var clusterrole = CreateClusterRole(config, resources);
        var clusterrolebinding = CreateClusterRoleBinding(config);

        var deployment = CreateDeployment(config);

        foreach (var item in resources)
        {
            var group = item.GetCustomAttribute<KubernetesEntityAttribute>();

            if (group == null || !group.Group.Equals(config.Group, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var crd = CreateCustomResourceDefinition(item, group);

            await Write(crd);
        }

        await Write(clusterrole);
        await Write(clusterrolebinding);
        await Write(ns);
        await Write(deployment);

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

    internal static V1CustomResourceDefinition CreateCustomResourceDefinition(Type item, KubernetesEntityAttribute group)
    {
        var scope = item.GetCustomAttribute<EntityScopeAttribute>() ?? EntityScopeAttribute.Default;
        var columns = item.GetCustomAttributes<AdditionalPrinterColumnAttribute>();

        var crdBuilder = KubernetesObjectBuilder.Create<V1CustomResourceDefinition>();
        crdBuilder
          .WithName($"{group.PluralName}.{group.Group}".ToLower())
          .WithSpec(x =>
          {
              x.WithGroup(group.Group)
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
                        schema.WithSchemaForType(item);
                        schema.WithServed(true);
                        schema.WithStorage(true);
                        schema.WithSubResources();
                        foreach (var column in columns)
                        {
                            schema.WithAdditionalPrinterColumn(column.Name, column.Type, column.Description, column.Path);
                        }

                    });
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
            .WithSpec(x =>
            {
                x.WithReplicas(1);
                x.WithRevisionHistory(0);
                x.WithSelector(x =>
                {
                    x.WithMatchLabel("operator", config.Name);
                });
                x.WithTemplate(temp =>
                {
                    temp.WithSpec(s =>
                    {
                        s.WithServiceAccountName("default");
                        s.WithSecurityContext(b =>
                            b.Add(x =>
                            {
                                x.RunAsNonRoot = true;
                                x.SeccompProfile = new()
                                {
                                    Type = "RuntimeDefault"
                                };
                            }));
                        s.AddContainer(config.Name, c =>
                        {
                            c.AddEnvFromObjectField("NAMESPACE", "metadata.name");
                            c.WithImage(config.Container.FullImage);
                            c.WithResources(
                                limits: new()
                                {
                                    ["cpu"] = new ResourceQuantity("100m"),
                                    ["memory"] = new ResourceQuantity("1Gi")
                                },
                                requests: new()
                                {
                                    ["cpu"] = new ResourceQuantity("100m"),
                                    ["memory"] = new ResourceQuantity("512Mi")
                                });
                            c.WithSecurityContext(b =>
                             {
                                 b.WithAllowPrivilegeEscalation(false);
                                 b.WithRunAsNonRoot();
                                 b.WithRunAsUser(2024);
                                 b.WithRunAsGroup(2024);
                                 b.WithCapabilities(cap =>
                                 {
                                     cap.Drop("ALL");
                                 });
                             });
                        });
                    });
                });
            });
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

    internal static V1ClusterRole CreateClusterRole(OperatorConfiguration config, IEnumerable<Type> resources)
    {
        var clusterrole = KubernetesObjectBuilder.Create<V1ClusterRole>()
                    .WithName($"{config.Name}-role");

        clusterrole.AddRule(x =>
               x.WithApiGroups("")
                .WithResources("pods", "pods/log", "services", "endpoints")
                .WithVerbs("get", "list", "watch")
         );

        clusterrole.AddRule(x =>
            x.WithApiGroups("coordination.k8s.io")
             .WithResources("leases")
             .WithVerbs("create", "update", "get")
        );

        var rules = resources
            .Select(x => x.GetCustomAttributes<KubernetesEntityAttribute>().First())
            .GroupBy(x => x.Group)
            .ToList();

        foreach (var item in rules)
        {
            clusterrole.AddRule(x =>
                x.WithApiGroups(item.Key)
                 .WithResources([.. item.Select(x => x.PluralName)])
                 .WithVerbs("*")
            );
            clusterrole.AddRule(x =>
                x.WithApiGroups(item.Key)
                 .WithResources([.. item.Select(x => $"{x.PluralName}/status")])
                 .WithVerbs("get", "update", "patch")
            );
        }

        return clusterrole.Build();
    }

}
