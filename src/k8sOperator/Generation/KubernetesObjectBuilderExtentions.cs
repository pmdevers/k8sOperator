using k8s.Models;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace k8s.Operator.Generation;

public static class KubernetesObjectBuilderExtentions
{
    extension<T>(IObjectBuilder<T> builder)
        where T : IMetadata<V1ObjectMeta>
    {
        public IObjectBuilder<T> WithName(string name)
        {
            builder.Add(x =>
            {
                x.Metadata.Name = name;
            });
            return builder;
        }
        public IObjectBuilder<T> WithNamespace(string ns)
        {
            builder.Add(x =>
            {
                x.Metadata.SetNamespace(ns);
            });
            return builder;
        }
        public IObjectBuilder<T> WithAnnotation(string key, string value)
        {
            builder.Add(x =>
            {
                x.Metadata.Annotations ??= new Dictionary<string, string>();
                x.Metadata.Annotations[key] = value;
            });
            return builder;
        }
        public IObjectBuilder<T> WithLabel(string key, string value)
        {
            builder.Add(x =>
            {
                x.Metadata.Labels ??= new Dictionary<string, string>();
                x.Metadata.Labels[key] = value;
            });
            return builder;
        }
        public IObjectBuilder<T> WithFinalizer(string finalizer)
        {
            builder.Add(x =>
            {
                x.Metadata.Finalizers ??= [];
                if (!x.Metadata.Finalizers.Contains(finalizer))
                {
                    x.Metadata.Finalizers.Add(finalizer);
                }
            });
            return builder;
        }
        public IObjectBuilder<T> RemoveFinalizer(string finalizer)
        {
            builder.Add(x =>
            {
                x.Metadata.Finalizers?.Remove(finalizer);
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1Deployment
    {
        public IObjectBuilder<T> WithSpec(Action<IObjectBuilder<V1DeploymentSpec>> customize)
        {
            builder.Add(x =>
            {
                x.Spec ??= new V1DeploymentSpec();
                var specBuilder = ObjectBuilder.Create(x.Spec);
                customize(specBuilder);
                x.Spec = specBuilder.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1DeploymentSpec
    {
        public IObjectBuilder<T> WithReplicas(int replicas)
            => builder.Add(x => x.Replicas = replicas);

        public IObjectBuilder<T> WithRevisionHistory(int revisionHistoryLimit)
            => builder.Add(x => x.RevisionHistoryLimit = revisionHistoryLimit);

        public IObjectBuilder<T> WithSelector(Action<IObjectBuilder<V1LabelSelector>> customize)
        {
            builder.Add(x =>
            {
                x.Selector ??= new V1LabelSelector();
                var selectorBuilder = ObjectBuilder.Create(x.Selector);
                customize(selectorBuilder);
                x.Selector = selectorBuilder.Build();
            });
            return builder;
        }

        public IObjectBuilder<T> WithTemplate(Action<IObjectBuilder<V1PodTemplateSpec>> customize)
        {
            builder.Add(x =>
            {
                x.Template ??= new V1PodTemplateSpec();
                var templateBuilder = ObjectBuilder.Create(x.Template);
                templateBuilder.Add(x => x.Metadata ??= new V1ObjectMeta());
                customize(templateBuilder);
                x.Template = templateBuilder.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1Service
    {
        public IObjectBuilder<T> WithSpec(Action<IObjectBuilder<V1ServiceSpec>> customize)
        {
            builder.Add(x =>
            {
                x.Spec ??= new V1ServiceSpec();
                var specBuilder = ObjectBuilder.Create(x.Spec);
                customize(specBuilder);
                x.Spec = specBuilder.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1ServiceSpec
    {
        public IObjectBuilder<T> WithType(string type)
        {
            builder.Add(x => x.Type = type);
            return builder;
        }
        public IObjectBuilder<T> WithPort(string name, int port, int targetPort, string protocol = "TCP")
        {
            builder.Add(x =>
            {
                x.Ports ??= [];
                x.Ports.Add(new V1ServicePort
                {
                    Name = name,
                    Port = port,
                    TargetPort = targetPort,
                    Protocol = protocol,
                });
            });
            return builder;
        }
        public IObjectBuilder<T> WithSelector(string key, string value)
        {
            builder.Add(x =>
            {
                x.Selector ??= new Dictionary<string, string>();
                x.Selector[key] = value;
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1PodTemplateSpec
    {
        public IObjectBuilder<T> WithMetadata(Action<IObjectBuilder<V1ObjectMeta>> customize)
        {
            builder.Add(x =>
            {
                x.Metadata ??= new V1ObjectMeta();
                var metadataBuilder = ObjectBuilder.Create(x.Metadata);
                customize(metadataBuilder);
                x.Metadata = metadataBuilder.Build();
            });
            return builder;
        }
        public IObjectBuilder<T> WithSpec(Action<IObjectBuilder<V1PodSpec>> customize)
        {
            builder.Add(x =>
            {
                x.Spec ??= new V1PodSpec();
                var specBuilder = ObjectBuilder.Create(x.Spec);
                customize(specBuilder);
                x.Spec = specBuilder.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1PodSpec
    {
        public IObjectBuilder<T> AddContainer(string name, Action<IObjectBuilder<V1Container>> customize)
        {
            builder.Add(x =>
            {
                x.Containers ??= [];
                var existingContainer = x.Containers.FirstOrDefault(c => c.Name == name);
                var containerBuilder =
                    ObjectBuilder.Create(existingContainer ?? new V1Container())
                        .Add(x => x.Name = name);

                customize(containerBuilder);
                x.Containers = [.. x.Containers.Where(c => c.Name != name), containerBuilder.Build()];
            });
            return builder;
        }

        public IObjectBuilder<T> WithNodeSelector(string key, string value)
        {
            builder.Add(x =>
            {
                x.NodeSelector ??= new Dictionary<string, string>();
                x.NodeSelector[key] = value;
            });
            return builder;
        }

        public IObjectBuilder<T> WithServiceAccountName(string serviceAccountName)
        {
            builder.Add(x => x.ServiceAccountName = serviceAccountName);
            return builder;
        }

        public IObjectBuilder<T> WithSecurityContext(Action<IObjectBuilder<V1PodSecurityContext>> customize)
        {
            builder.Add(x =>
            {
                x.SecurityContext ??= new V1PodSecurityContext();
                var securityContextBuilder = ObjectBuilder.Create(x.SecurityContext);
                customize(securityContextBuilder);
                x.SecurityContext = securityContextBuilder.Build();
            });
            return builder;
        }

        public IObjectBuilder<T> WithContainer(Action<IObjectBuilder<V1Container>> customize)
        {
            builder.Add(x =>
            {
                var containerBuilder = ObjectBuilder.Create(new V1Container());
                customize(containerBuilder);
                x.Containers ??= [];
                x.Containers.Add(containerBuilder.Build());
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1Container
    {
        public IObjectBuilder<T> WithImage(string image)
        {
            builder.Add(x => x.Image = image);
            return builder;
        }
        public IObjectBuilder<T> WithPort(int containerPort, string protocol = "TCP")
        {
            builder.Add(x =>
            {
                x.Ports ??= [];
                x.Ports.Add(new V1ContainerPort
                {
                    ContainerPort = containerPort,
                    Protocol = protocol
                });
            });
            return builder;
        }
        public IObjectBuilder<T> WithResources(
            Dictionary<string, ResourceQuantity>? limits = null,
            Dictionary<string, ResourceQuantity>? requests = null)
        {
            builder.Add(x =>
            {
                x.Resources ??= new V1ResourceRequirements();
                x.Resources.Limits = limits;
                x.Resources.Requests = requests;
            });
            return builder;
        }

        public IObjectBuilder<T> AddEnvFromObjectField(string name, string path)
        {
            builder.Add(x =>
            {
                x.Env ??= [];
                x.Env.Add(new()
                {
                    Name = name,
                    ValueFrom = new()
                    {
                        FieldRef = new V1ObjectFieldSelector()
                        {
                            FieldPath = path
                        }
                    }
                });
            });
            return builder;
        }
        public IObjectBuilder<T> AddEnvFromSecretKey(string name, string key, string secretName)
        {
            builder.Add(x =>
            {
                x.Env ??= [];
                x.Env.Add(new()
                {
                    Name = name,
                    ValueFrom = new()
                    {
                        SecretKeyRef = new V1SecretKeySelector()
                        {
                            Key = key,
                            Name = secretName,
                        }
                    }
                });
            });
            return builder;
        }
        public IObjectBuilder<T> AddEnvFromResourceField(string name, string path)
        {
            builder.Add(x =>
            {
                x.Env ??= [];
                x.Env.Add(new()
                {
                    Name = name,
                    ValueFrom = new()
                    {
                        ResourceFieldRef = new V1ResourceFieldSelector
                        {
                            Resource = path
                        }
                    }
                });
            });
            return builder;
        }
        public IObjectBuilder<T> AddEnvFromConfigMapKey(string name, string key, string configMapName)
        {
            builder.Add(x =>
            {
                x.Env ??= [];
                x.Env.Add(new()
                {
                    Name = name,
                    ValueFrom = new()
                    {
                        ConfigMapKeyRef = new V1ConfigMapKeySelector
                        {
                            Key = key,
                            Name = configMapName
                        }
                    }
                });
            });
            return builder;
        }
        public IObjectBuilder<T> WithSecurityContext(Action<IObjectBuilder<V1SecurityContext>> customize)
        {
            builder.Add(x =>
            {
                x.SecurityContext ??= new V1SecurityContext();
                var securityContextBuilder = ObjectBuilder.Create(x.SecurityContext);
                customize(securityContextBuilder);
                x.SecurityContext = securityContextBuilder.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1SecurityContext
    {
        public IObjectBuilder<T> WithAllowPrivilegeEscalation(bool allow = true)
        {
            builder.Add(x => x.AllowPrivilegeEscalation = allow);
            return builder;
        }

        public IObjectBuilder<T> WithRunAsUser(long runAsUser)
        {
            builder.Add(x => x.RunAsUser = runAsUser);
            return builder;
        }
        public IObjectBuilder<T> WithRunAsGroup(long runAsGroup)
        {
            builder.Add(x => x.RunAsGroup = runAsGroup);
            return builder;
        }
        public IObjectBuilder<T> WithRunAsNonRoot(bool runAsNonRoot = true)
        {
            builder.Add(x => x.RunAsNonRoot = runAsNonRoot);
            return builder;
        }

        public IObjectBuilder<T> WithCapabilities(Action<IObjectBuilder<V1Capabilities>> customize)
        {
            builder.Add(x =>
            {
                x.Capabilities ??= new V1Capabilities();
                var capabilitiesBuilder = ObjectBuilder.Create(x.Capabilities);
                customize(capabilitiesBuilder);
                x.Capabilities = capabilitiesBuilder.Build();
            });
            return builder;
        }

        public IObjectBuilder<T> WithSeccompProfile(string type, string? localhostProfile = null)
        {
            builder.Add(x =>
            {
                x.SeccompProfile = new V1SeccompProfile
                {
                    Type = type,
                    LocalhostProfile = localhostProfile
                };
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1Capabilities
    {
        public IObjectBuilder<T> Add(string capability)
        {
            builder.Add(x =>
            {
                x.Add ??= [];
                if (!x.Add.Contains(capability))
                {
                    x.Add.Add(capability);
                }
            });
            return builder;
        }
        public IObjectBuilder<T> Drop(string capability)
        {
            builder.Add(x =>
            {
                x.Drop ??= [];
                if (!x.Drop.Contains(capability))
                {
                    x.Drop.Add(capability);
                }
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1LabelSelector
    {
        public IObjectBuilder<T> WithMatchLabel(string key, string value)
        {
            builder.Add(x =>
            {
                x.MatchLabels ??= new Dictionary<string, string>();
                x.MatchLabels[key] = value;
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1CustomResourceDefinition
    {
        public IObjectBuilder<T> WithSpec(Action<IObjectBuilder<V1CustomResourceDefinitionSpec>> customize)
        {
            builder.Add(x =>
            {
                x.Spec ??= new V1CustomResourceDefinitionSpec();
                var specBuilder = ObjectBuilder.Create(x.Spec);
                customize(specBuilder);
                x.Spec = specBuilder.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1CustomResourceDefinitionSpec
    {
        public IObjectBuilder<T> WithGroup(string group)
        {
            builder.Add(x =>
            {
                x.Group = group;
            });
            return builder;
        }
        public IObjectBuilder<T> WithNames(string kind, string kindList, string plural, string singular)
        {
            builder.Add(x =>
            {
                x.Names = new V1CustomResourceDefinitionNames
                {
                    Kind = kind,
                    ListKind = kindList,
                    Plural = plural,
                    Singular = singular
                };
            });
            return builder;
        }
        public IObjectBuilder<T> WithScope(string scope)
        {
            builder.Add(x =>
            {
                x.Scope = scope;
            });
            return builder;
        }
        public IObjectBuilder<T> WithVersion(string version, Action<IObjectBuilder<V1CustomResourceDefinitionVersion>>? customize = null)
        {
            builder.Add(x =>
            {
                var versionBuilder = ObjectBuilder.Create(new V1CustomResourceDefinitionVersion());
                versionBuilder.Add(v => v.Name = version);
                customize?.Invoke(versionBuilder);
                x.Versions ??= [];
                x.Versions.Add(versionBuilder.Build());
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1CustomResourceDefinitionVersion
    {
        public IObjectBuilder<T> WithSchemaForType(Type type)
        {
            builder.Add(x =>
            {
                var schemaBuilder = ObjectBuilder.Create(x.Schema ?? new());
                schemaBuilder.ForType(type);
                x.Schema = schemaBuilder.Build();
            });
            return builder;
        }

        public IObjectBuilder<T> WithSubResources()
        {
            builder.Add(x =>
            {
                x.Subresources = new V1CustomResourceSubresources
                {
                    Status = new(),
                };
            });
            return builder;
        }

        public IObjectBuilder<T> WithServed(bool served)
        {
            builder.Add(x => x.Served = served);
            return builder;
        }

        public IObjectBuilder<T> WithStorage(bool storage)
        {
            builder.Add(x => x.Storage = storage);
            return builder;
        }

        public IObjectBuilder<T> WithAdditionalPrinterColumn(string name, string type, string description, string jsonPath)
        {
            builder.Add(x =>
            {
                x.AdditionalPrinterColumns ??= [];
                x.AdditionalPrinterColumns.Add(new V1CustomResourceColumnDefinition
                {
                    Name = name,
                    Type = type,
                    Description = description,
                    JsonPath = jsonPath
                });
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1CustomResourceValidation
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        public IObjectBuilder<T> ForType(Type type)
        {
            var s = ObjectBuilder.Create<V1JSONSchemaProps>();

            s.OfType("object");

            var status = type.GetProperty("Status")?.PropertyType;
            var spec = type.GetProperty("Spec")?.PropertyType;

            if (status is not null)
            {
                s.WithProperty("status", sub =>
                {
                    sub.ObjectType(status);
                });
            }

            if (spec is not null)
            {
                s.WithProperty("spec", sub =>
                {
                    sub.ObjectType(spec);
                });
            }

            builder.Add(x =>
            {
                x.OpenAPIV3Schema = s.Build();
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1JSONSchemaProps
    {
        public IObjectBuilder<T> WithProperty(string name, Action<IObjectBuilder<V1JSONSchemaProps>> customize)
        {
            builder.Add(x =>
            {
                var propBuilder = ObjectBuilder.Create(new V1JSONSchemaProps());
                customize(propBuilder);
                x.Properties ??= new Dictionary<string, V1JSONSchemaProps>();
                x.Properties[$"{name[..1].ToLowerInvariant()}{name[1..]}"] = propBuilder.Build();
            });
            return builder;
        }

        public IObjectBuilder<T> OfType(Type type, bool nullable = false, string? pattern = null, object? defaultValue = null)
        {
            if (type == typeof(string))
            {
                builder.Add(x =>
                {
                    x.Type = "string";
                    x.Nullable = nullable;

                    if (pattern is not null)
                        x.Pattern = pattern;

                    if (defaultValue is not null)
                        x.DefaultProperty = defaultValue;
                });
                return builder;
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                builder.Add(x =>
                {
                    x.Type = "integer";
                    x.Nullable = nullable;
                });
                return builder;
            }
            else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                builder.Add(x =>
                {
                    x.Type = "number";
                    x.Nullable = nullable;
                });
                return builder;
            }
            else if (type == typeof(bool))
            {
                builder.Add(x =>
                {
                    x.Type = "boolean";
                    x.Nullable = nullable;
                });
                return builder;
            }
            if (type == typeof(DateTime))
            {
                builder.Add(x =>
                {
                    x.Type = "string";
                    x.Nullable = nullable;
                });
                return builder;
            }

            if (type.Name == typeof(Nullable<>).Name && type.GenericTypeArguments.Length == 1)
            {
                return builder.OfType(type.GenericTypeArguments[0], true);
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var itemSchema = ObjectBuilder.Create<V1JSONSchemaProps>();
                itemSchema.OfType(elementType);

                builder.Add(x =>
                {
                    x.Type = "array";
                    x.Items = itemSchema.Build();
                    x.Nullable = nullable;
                });
                return builder;
            }

            if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(List<>) ||
             type.GetGenericTypeDefinition() == typeof(IList<>) ||
             type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
             type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var elementType = type.GetGenericArguments()[0];
                var itemSchema = ObjectBuilder.Create<V1JSONSchemaProps>()
                    .OfType(elementType);


                builder.Add(x =>
                {
                    x.Type = "array";
                    x.Items = itemSchema.Build();
                    x.Nullable = nullable;
                });
                return builder;
            }

            return type.BaseType?.FullName switch
            {
                "System.Object" => builder.ObjectType(type),
                //"System.ValueType" => context.MapValueType(type),
                "System.Enum" => builder.EnumType(type),
                _ => throw new InvalidOperationException($"Invalid type: '{type}'."),
            };
        }

        private IObjectBuilder<T> ObjectType(Type type)
        {
            switch (type.FullName)
            {
                case "k8s.Models.V1ObjectMeta":
                    builder.Add(x =>
                    {
                        x.Type = "object";
                        x.Nullable = false;
                    });
                    return builder;
                case "k8s.Models.IntstrIntOrString":
                    builder.Add(x =>
                    {
                        x.XKubernetesIntOrString = true;
                        x.Nullable = false;
                    });
                    return builder;

                default:
                    if (typeof(IKubernetesObject).IsAssignableFrom(type) &&
                        type is { IsAbstract: false, IsInterface: false })
                    {
                        builder.Add(x =>
                        {
                            x.Type = "object";
                            x.Properties = null;
                            x.XKubernetesPreserveUnknownFields = true;
                            x.XKubernetesEmbeddedResource = true;
                            x.Nullable = false;
                        });
                        return builder;
                    }

                    builder.OfType("object");
                    builder.IsNullable(false);
                    foreach (var prop in type.GetProperties())
                    {
                        var pattern = prop.GetCustomAttribute<PatternAttribute>();
                        var defaultValue = prop.GetCustomAttribute<DefaultAttribute>();

                        builder.WithProperty(prop.Name, s => s.OfType(prop.PropertyType, prop.PropertyType.IsNullable(), pattern?.Pattern, defaultValue?.Default));
                    }

                    builder.WithRequired(type.GetProperties()
                        .Where(x => !x.PropertyType.IsNullable())
                        .Select(x => x.Name).ToList() switch
                    {
                        { Count: > 0 } p => p,
                        _ => null,
                    });

                    return builder;
            }
        }

        public IObjectBuilder<T> WithRequired(IEnumerable<string>? names)
        {
            builder.Add(x => x.Required = names?.Select(name => $"{name[..1].ToLowerInvariant()}{name[1..]}").ToList());
            return builder;
        }

        public IObjectBuilder<T> IsNullable(bool nullable)
        {
            builder.Add(x => x.Nullable = nullable);
            return builder;
        }

        public IObjectBuilder<T> EnumType(Type type)
        {
            if (!type.IsEnum)
                throw new InvalidOperationException($"Type '{type}' is not an enum.");

            builder.Add(x =>
            {
                x.Type = "string";
                x.EnumProperty = [.. Enum.GetNames(type).Cast<object>()];
            });
            return builder;
        }

        public IObjectBuilder<T> OfType(string type)
        {
            builder.Add(x =>
            {
                x.Type = type;
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1ClusterRoleBinding
    {
        public IObjectBuilder<T> WithRoleRef(string apiGroup, string kind, string name)
        {
            builder.Add(x =>
            {
                x.RoleRef = new()
                {
                    ApiGroup = apiGroup,
                    Kind = kind,
                    Name = name
                };
            });
            return builder;
        }

        public IObjectBuilder<T> WithSubject(string kind, string name, string? apiGroup = null, string? ns = null)
        {
            builder.Add(x =>
            {
                x.Subjects ??= [];
                x.Subjects.Add(new()
                {
                    ApiGroup = apiGroup,
                    Kind = kind,
                    Name = name,
                    NamespaceProperty = ns
                });
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1ClusterRole
    {
        public IObjectBuilder<T> AddRule(Action<IObjectBuilder<V1PolicyRule>> customize)
        {
            builder.Add(x =>
            {
                var ruleBuilder = ObjectBuilder.Create(new V1PolicyRule());
                customize(ruleBuilder);
                x.Rules ??= [];
                x.Rules.Add(ruleBuilder.Build());
            });
            return builder;
        }
    }

    extension<T>(IObjectBuilder<T> builder)
        where T : V1PolicyRule
    {
        public IObjectBuilder<T> WithApiGroups(string[] apiGroups)
        {
            builder.Add(x =>
            {
                x.ApiGroups = apiGroups;
            });
            return builder;
        }
        public IObjectBuilder<T> WithResources(string[] resources)
        {
            builder.Add(x =>
            {
                x.Resources = resources;
            });
            return builder;
        }
        public IObjectBuilder<T> WithVerbs(string[] verbs)
        {
            builder.Add(x =>
            {
                x.Verbs = verbs;
            });
            return builder;
        }
    }

    extension(Type type)
    {
        public bool IsNullable()
        {
            return !type.IsValueType ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
    }


}
