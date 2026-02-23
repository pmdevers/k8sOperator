using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace k8s.Operator.Configuration;

public class OperatorConfigurationProvider(
    IConfiguration? configuration = null,
    Assembly? assembly = null)
{
    private readonly Assembly _assembly = assembly
        ?? Assembly.GetEntryAssembly()
        ?? Assembly.GetExecutingAssembly();
    public OperatorConfiguration Build()
    {
        var config = new OperatorConfiguration();
        // 1. Start with assembly attributes (lowest priority)
        ApplyAssemblyAttributes(config);
        // 2. Apply configuration (e.g., appsettings.json)
        ApplyConfiguration(config);
        // 3. OperatorBuilder can override in AddOperator() (highest priority, done by caller)
        return config;
    }
    private void ApplyAssemblyAttributes(OperatorConfiguration config)
    {
        var versionAttrib = _assembly.GetCustomAttribute<OperatorVersionAttribute>();
        if (versionAttrib != null && !string.IsNullOrEmpty(versionAttrib.Version))
        {
            config.Version = versionAttrib.Version;
        }
        // Read OperatorName from assembly attribute
        var operatorNameAttr = _assembly.GetCustomAttribute<OperatorNameAttribute>();
        if (operatorNameAttr != null && !string.IsNullOrEmpty(operatorNameAttr.OperatorName))
        {
            config.Name = operatorNameAttr.OperatorName.ToLowerInvariant();
        }
        // Read Namespace from assembly attribute
        var namespaceAttr = _assembly.GetCustomAttribute<OperatorNamespaceAttribute>();
        if (namespaceAttr != null && !string.IsNullOrEmpty(namespaceAttr.Namespace))
        {
            config.Namespace = namespaceAttr.Namespace;
        }

        var containerAttr = _assembly.GetCustomAttribute<ContainerAttribute>();
        if (containerAttr != null)
        {
            config.Container = new()
            {
                Registry = containerAttr.Registry,
                Organization = containerAttr.Organization,
                Image = containerAttr.Image,
                Tag = containerAttr.Tag
            };
        }
    }
    private void ApplyConfiguration(OperatorConfiguration config)
    {
        if (configuration == null)
        {
            return;
        }

        var version = configuration["Operator:Version"];
        if (!string.IsNullOrEmpty(version))
        {
            config.Version = version;
        }
        var name = configuration["Operator:Name"];
        if (!string.IsNullOrEmpty(name))
        {
            config.Name = name;
        }
        var ns = configuration["Operator:Namespace"];
        if (!string.IsNullOrEmpty(ns))
        {
            config.Namespace = ns;
        }
        var containerSection = configuration.GetSection("Operator:Container");
        if (containerSection != null)
        {
            var registery = containerSection["Registry"];
            if (!string.IsNullOrEmpty(registery))
            {
                config.Container.Registry = containerSection["Registry"];
            }

            var organization = containerSection["Organization"];
            if (!string.IsNullOrEmpty(organization))
            {
                config.Container.Organization = organization;
            }

            var image = containerSection["Image"];
            if (!string.IsNullOrEmpty(image))
            {
                config.Container.Image = image;
            }

            var tag = containerSection["Tag"];
            if (!string.IsNullOrEmpty(tag))
            {
                config.Container.Tag = tag;
            }

            var digest = containerSection["Digest"];
            if (!string.IsNullOrEmpty(digest))
            {
                config.Container.Digest = digest;
            }
        }
    }
}
