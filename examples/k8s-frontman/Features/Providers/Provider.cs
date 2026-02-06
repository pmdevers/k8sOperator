using k8s.Frontman.Features.Providers.Azure;
using k8s.Frontman.Features.Providers.File;
using k8s.Models;
using k8s.Operator.Generation.Attributes;
using k8s.Operator.Metadata;
using k8s.Operator.Models;

namespace k8s.Frontman.Features.Providers;

[KubernetesEntity(Group = "frontman.io", ApiVersion = "v1alpha1", Kind = "Provider", PluralName = "providers")]
[AdditionalPrinterColumn(Name = "Refresh", Path = ".spec.interval", Description = "", Type = "string")]
[AdditionalPrinterColumn(Name = "Releases", Path = ".status.numberOfReleases", Description = "", Type = "string")]
public class Provider : CustomResource<Provider.Specs, Provider.State>
{
    public class Specs
    {
        public FileProviderOptions? File { get; set; }
        public AzureBlobFileProviderOptions? AzureBlob { get; set; }

        [ResyncInterval]
        [Default("5m")]
        public string Interval { get; set; } = "5m";
    }

    public class State
    {
        public int NumberOfReleases { get; set; } = 0;
        public string[] Versions { get; set; } = [];
    }
}
