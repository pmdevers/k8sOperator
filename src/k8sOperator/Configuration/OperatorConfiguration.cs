namespace k8s.Operator.Configuration;

/// <summary>
/// Configuration for the Kubernetes Operator.
/// Can be populated from assembly attributes, appsettings.json, or OperatorBuilder.
/// </summary>
public class OperatorConfiguration
{
    /// <summary>
    /// Gets or sets the URL used to check for application updates.
    /// </summary>
    public string? UpdateUrl { get; set; }

    /// <summary>
    /// Gets or sets the operator version. Defaults to "1.0.0" if not set via assembly attributes or configuration.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Name of the operator (e.g., "my-operator")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kubernetes namespace where the operator runs (e.g., "my-operator-system")
    /// </summary>
    public string Namespace { get; set; } = "default";

    /// <summary>
    /// Docker image registry (e.g., "ghcr.io")
    /// </summary>
    public string ContainerRegistry { get; set; } = "ghcr.io";

    /// <summary>
    /// Docker image repository (e.g., "company/my-operator")
    /// </summary>
    public string ContainerRepository { get; set; } = string.Empty;

    /// <summary>
    /// Docker image tag (e.g., "1.0.0" or "1.0.0-alpha"). Defaults to "latest" if not set.
    /// </summary>
    public string ContainerTag { get; set; } = string.Empty;

    /// <summary>
    /// Full container image (registry/repository:tag).
    /// Validates that ContainerRegistry and ContainerRepository are not empty.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when ContainerRegistry or ContainerRepository are null, empty, or whitespace.
    /// </exception>
    public string ContainerImage
    {
        get
        {
            var registry = ContainerRegistry?.Trim();
            var repository = ContainerRepository?.Trim();
            var tag = ContainerTag ?? "latest"; // Already handles default "latest"

            if (string.IsNullOrWhiteSpace(registry))
            {
                throw new InvalidOperationException(
                    "ContainerRegistry must be configured before accessing ContainerImage. " +
                    "Set it via assembly attributes, appsettings.json, or OperatorBuilder.");
            }

            if (string.IsNullOrWhiteSpace(repository))
            {
                throw new InvalidOperationException(
                    "ContainerRepository must be configured before accessing ContainerImage. " +
                    "Set it via assembly attributes, appsettings.json, or OperatorBuilder.");
            }

            return $"{registry}/{repository}:{tag}";
        }
    }

    /// <summary>
    /// Validates that all required configuration properties are set.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required configuration is missing.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException(
                "OperatorName must be configured. " +
                "Set it via assembly attributes, appsettings.json, or OperatorBuilder.");
        }

        if (string.IsNullOrWhiteSpace(Namespace))
        {
            throw new InvalidOperationException(
                "Namespace must be configured. " +
                "Set it via assembly attributes, appsettings.json, or OperatorBuilder.");
        }

        // Trigger ContainerImage validation
        _ = ContainerImage;
    }
}

