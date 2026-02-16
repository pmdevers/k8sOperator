using k8s.Frontman.Features.Providers.AzureBlob;
using k8s.Frontman.Features.Providers.File;
using k8s.Models;
using k8s.Operator.Generation;
using Microsoft.Extensions.FileProviders;

namespace k8s.Frontman.Features.Providers;

[KubernetesEntity(Group = KubeGroup, ApiVersion = KubeApiVersion, Kind = KubeKind, PluralName = KubePluralName)]
[AdditionalPrinterColumn("Refresh", "string", "", ".spec.interval")]
[AdditionalPrinterColumn("Releases", "string", "", ".status.numberOfReleases")]
public class V1Provider : IKubernetesObject<V1ObjectMeta>,
    ISpec<V1ProviderSpec>, IStatus<V1ProviderStatus>
{
    public const string KubeApiVersion = "v1";
    public const string KubeKind = "Provider";
    public const string KubeGroup = "frontman.io";
    public const string KubePluralName = "providers";

    public string ApiVersion { get; set; } = $"{KubeGroup}/{KubeApiVersion}";
    public string Kind { get; set; } = KubeKind;
    public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    public V1ProviderSpec Spec { get; set; } = new V1ProviderSpec();
    public V1ProviderStatus Status { get; set; } = new V1ProviderStatus();

    public IFileProvider? GetFileProvider()
        => Spec.Type switch
        {
            ProviderTypes.File => Spec.File?.Create(),
            ProviderTypes.AzureBlob => Spec.AzureBlob?.Create(),
            _ => null
        };

}
public class V1ProviderSpec
{
    public ProviderTypes Type { get; set; } = ProviderTypes.File;
    public FileProviderOptions? File { get; set; } = new();
    public AzureBlobFileProviderOptions? AzureBlob { get; set; } = new();

    [ResyncInterval]
    [Default("5m")]
    public string Interval { get; set; } = "5m";
}

public enum ProviderTypes
{
    File,
    AzureBlob
}

public class V1ProviderStatus
{
    public int NumberOfReleases { get; set; } = 0;
    public string[] Versions { get; set; } = [];
}
