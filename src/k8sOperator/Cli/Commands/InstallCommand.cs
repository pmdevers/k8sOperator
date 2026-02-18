using k8s.Models;
using k8s.Operator.Configuration;
using k8s.Operator.Generation;
using System.Reflection;

namespace k8s.Operator.Cli.Commands;

[OperatorCommand("install", "Install the operator", -1, "-i", "--install")]
public class InstallCommand(OperatorConfiguration config) : IOperatorCommand
{
    private readonly StringWriter _output = new();

    [Option(Name = "--output", Aliases = ["-o"], Description = "Output file path (default: stdout)", ValueName = "file")]
    public string? OutputFile { get; set; }

    [Option(Name = "--namespace", Aliases = ["-n"], Description = "Override the operator namespace", ValueName = "name")]
    public string? NamespaceOverride { get; set; }

    [Option(Name = "--skip-crds", Description = "Skip CustomResourceDefinition installation")]
    public bool SkipCrds { get; set; }

    [Option(Name = "--skip-deployment", Description = "Skip Deployment installation")]
    public bool SkipDeployment { get; set; }

    [Option(Name = "--skip-namespace", Description = "Skip Namespace installation")]
    public bool SkipNamespace { get; set; }

    public async Task<int> ExecuteAsync(string[] args)
    {
        // Apply namespace override if specified
        var effectiveNamespace = NamespaceOverride ?? config.Namespace;
        var effectiveConfig = config with { Namespace = effectiveNamespace };
        var resources = effectiveConfig.Install.Resources;

        if (!SkipCrds)
        {
            foreach (var item in resources)
            {
                var group = item.GetCustomAttribute<KubernetesEntityAttribute>();

                if (group == null)
                {
                    continue;
                }

                var crd = CreateCustomResourceDefinition(item, group);
                await Write(crd);
            }
        }

        var clusterrole = CreateClusterRole(effectiveConfig, resources);
        var clusterrolebinding = CreateClusterRoleBinding(effectiveConfig);

        await Write(clusterrole);
        await Write(clusterrolebinding);

        if (!SkipNamespace)
        {
            var ns = CreateNamespace(effectiveConfig);
            await Write(ns);
        }

        if (!SkipDeployment)
        {
            var deployment = CreateDeployment(effectiveConfig);
            await Write(deployment);
        }

        foreach (var item in effectiveConfig.Install.AdditionalObjects)
        {
            if (item is IKubernetesObject<V1ObjectMeta> obj)
            {
                if (string.IsNullOrEmpty(obj.Metadata.NamespaceProperty))
                {
                    obj.Metadata.NamespaceProperty = effectiveConfig.Namespace;
                }
            }

            await Write(item);
        }

        var output = _output.ToString();

        // Write to file or stdout
        if (!string.IsNullOrEmpty(OutputFile))
        {
            await File.WriteAllTextAsync(OutputFile, output);
            Console.WriteLine($"Installation manifests written to: {OutputFile}");
        }
        else
        {
            Console.WriteLine(output);
        }

        return 0;
    }

    private async Task Write(IKubernetesObject obj)
    {
        await _output.WriteLineAsync(KubernetesYaml.Serialize(obj));
        await _output.WriteLineAsync("---");
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

    private V1Namespace CreateNamespace(OperatorConfiguration config) =>
            KubernetesObjectBuilder.Create<V1Namespace>()
            .WithName(config.Namespace)
            .Add(x => config.Install.ConfigureNamespace?.Invoke(x))
            .Build();

    private V1Deployment CreateDeployment(OperatorConfiguration config)
        => KubernetesObjectBuilder.Create<V1Deployment>()
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
                    temp.WithLabel("operator", config.Name);
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
                            c.AddEnvFromObjectField("OPERATOR__NAMESPACE", "metadata.namespace");
                            c.WithImage(config.Container.FullImage());
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
            })
            .Add(x => config.Install.ConfigureDeployment?.Invoke(x))
        .Build();

    internal static V1ClusterRoleBinding CreateClusterRoleBinding(OperatorConfiguration config)
        => KubernetesObjectBuilder.Create<V1ClusterRoleBinding>()
            .WithName($"{config.Name}-role-binding")
            .WithRoleRef("rbac.authorization.k8s.io", "ClusterRole", $"{config.Name}-role")
            .WithSubject(kind: "ServiceAccount", name: "default", ns: config.Namespace)
            .Add(x => config.Install.ConfigureClusterRoleBinding?.Invoke(x))
            .Build();


    internal static V1ClusterRole CreateClusterRole(OperatorConfiguration config, IEnumerable<Type> resources)
    {
        var clusterrole = KubernetesObjectBuilder.Create<V1ClusterRole>()
                    .WithName($"{config.Name}-role");

        clusterrole.AddRule(x =>
               x.WithApiGroups([string.Empty])
                .WithResources(["pods", "pods/log", "services", "endpoints"])
                .WithVerbs(["get", "list", "watch"])
         );

        clusterrole.AddRule(x =>
            x.WithApiGroups(["coordination.k8s.io"])
             .WithResources(["leases"])
             .WithVerbs(["create", "update", "get"])
        );

        var rules = resources
            .Select(x => x.GetCustomAttribute<KubernetesEntityAttribute>())
            .Where(x => x is not null)
            .Select(x => x!)
            .GroupBy(x => x.Group)
            .ToList();

        foreach (var item in rules)
        {
            var apiGroup = item.Key ?? string.Empty;
            clusterrole.AddRule(x =>
                x.WithApiGroups([apiGroup])
                 .WithResources([.. item.Select(x => x.PluralName)])
                 .WithVerbs(["*"])
            );
            clusterrole.AddRule(x =>
                x.WithApiGroups([apiGroup])
                 .WithResources([.. item.Select(x => $"{x.PluralName}/status")])
                 .WithVerbs(["get", "update", "patch"])
            );
        }

        clusterrole.Add(x => config.Install.ConfigureClusterRole?.Invoke(x));

        return clusterrole.Build();
    }

}
