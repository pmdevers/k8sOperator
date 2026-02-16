using k8s.Models;
using System.Text.RegularExpressions;

namespace k8s.Operator.Configuration;

public partial record OperatorConfiguration
{
    public string Name { get; set; } = "my-operator";
    public string Group { get; set; } = "simplicity.io";
    public string Version { get; set; } = "0.1.0";
    public string Namespace { get; set; } = "default";
    public LeaseConfiguration Lease { get; set; } = new();
    public ContainerConfiguration Container { get; set; } = new();
    public InstallConfiguration Install { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Operator name must be provided.", nameof(Name));

        if (!IsValidKubernetesName(Name))
            throw new ArgumentException(
                "Operator name must be a valid Kubernetes name: lowercase alphanumeric characters, '-' or '.', " +
                "must start and end with an alphanumeric character, and be 63 characters or less.",
                nameof(Name));

        if (string.IsNullOrWhiteSpace(Group))
            throw new ArgumentException("Operator group must be provided.", nameof(Group));
        if (string.IsNullOrWhiteSpace(Version))
            throw new ArgumentException("Operator version must be provided.", nameof(Version));
        if (string.IsNullOrWhiteSpace(Namespace))
            throw new ArgumentException("Operator namespace must be provided.", nameof(Namespace));
    }

    private static bool IsValidKubernetesName(string name)
    {
        if (name.Length > 63)
            return false;

        // Kubernetes name pattern: lowercase alphanumeric, '-' or '.', must start and end with alphanumeric
        var regex = KubernetesName();
        return regex.IsMatch(name);
    }

    public class ContainerConfiguration
    {
        public string Registry { get; set; } = "ghcr.io";
        public string Repository { get; set; } = "default";
        public string Image { get; set; } = "my-operator";
        public string Tag { get; set; } = "0.1.0";
        public string FullImage => $"{Registry}/{Repository}/{Image}:{Tag}";
    }
    public class LeaseConfiguration
    {
        public string? LeaseName { get; set; }
        public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan RenewDeadline { get; set; } = TimeSpan.FromSeconds(20);
        public TimeSpan RetryPeriod { get; set; } = TimeSpan.FromSeconds(10);
    }

    public class InstallConfiguration
    {
        public List<Type> Resources { get; set; } = [];
        public Action<V1Namespace>? ConfigureNamespace { get; set; }
        public Action<V1Deployment>? ConfigureDeployment { get; set; }
        public Action<V1ClusterRoleBinding>? ConfigureClusterRoleBinding { get; set; }
        public Action<V1ClusterRole>? ConfigureClusterRole { get; set; }
        public List<IKubernetesObject> AdditionalObjects { get; set; } = [];

    }

    [GeneratedRegex(@"^[a-z0-9]([-a-z0-9]*[a-z0-9])?(\.[a-z0-9]([-a-z0-9]*[a-z0-9])?)*$", RegexOptions.Compiled)]
    private static partial Regex KubernetesName();
}

