using System.Reflection;

namespace Simplicity.Operator.Configuration;

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
            config.Name = operatorNameAttr.OperatorName;
        }
        // Read Namespace from assembly attribute
        var namespaceAttr = _assembly.GetCustomAttribute<OperatorNamespaceAttribute>();
        if (namespaceAttr != null && !string.IsNullOrEmpty(namespaceAttr.Namespace))
        {
            config.Namespace = namespaceAttr.Namespace;
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
    }
}
