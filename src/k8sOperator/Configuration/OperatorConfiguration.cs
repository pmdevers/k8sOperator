using k8s.Models;
using System.Text.RegularExpressions;

namespace k8s.Operator.Configuration;

public partial record OperatorConfiguration
{
    public string Name { get; set; } = "my-operator";
    public string Version { get; set; } = "0.0.1";
    public string? Namespace { get; set; }

    public KubernetesClientConfiguration? Kubernetes { get; set; }
    public LeaseConfiguration Lease { get; set; } = new();
    public ContainerConfiguration Container { get; set; } = new();
    public InstallConfiguration Install { get; set; } = new();

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(Namespace);

        ValidateKubernetesName(Name, nameof(Name), "Operator name");
        ValidateKubernetesName(Namespace, nameof(Namespace), "Namespace");
    }

    private static void ValidateKubernetesName(string value, string paramName, string displayName)
    {
        if (!IsValidKubernetesName(value))
        {
            throw new ArgumentException(
                $"{displayName} must be a valid Kubernetes name: lowercase alphanumeric characters, '-' or '.', " +
                "must start and end with an alphanumeric character, and be 63 characters or less.",
                paramName);
        }
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
        public string? Registry { get; set; }
        public string? Organization { get; set; }
        public string Image { get; set; } = string.Empty;
        public string? Tag { get; set; }
        public string? Digest { get; set; }
        public string FullImage()
        {
            var parts = new List<string>();

            // Add registry if provided
            if (!string.IsNullOrWhiteSpace(Registry))
            {
                parts.Add(Registry);
            }

            // Add organization if provided
            if (!string.IsNullOrWhiteSpace(Organization))
            {
                parts.Add(Organization);
            }

            // Add image name (always required)
            parts.Add(Image);

            // Construct base image path
            var imagePath = string.Join('/', parts);

            // Add digest or tag
            if (!string.IsNullOrWhiteSpace(Digest))
            {
                return $"{imagePath}@{Digest}";
            }

            var tag = !string.IsNullOrWhiteSpace(Tag) ? Tag : "latest";
            return $"{imagePath}:{tag}".ToLowerInvariant();
        }
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

