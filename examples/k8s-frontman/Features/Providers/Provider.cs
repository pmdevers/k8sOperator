using k8s.Frontman.Features.Providers.Azure;
using k8s.Frontman.Features.Providers.File;
using k8s.Models;
using k8s.Operator.Models;

namespace k8s.Frontman.Features.Providers;

[KubernetesEntity(Group = "frontman.io", ApiVersion = "v1alpha1", Kind = "Provider", PluralName = "providers")]
public class Provider : CustomResource<Provider.Specs, Provider.State>
{
    public class Specs
    {
        public FileProviderOptions? File { get; set; }
        public AzureBlobFileProviderOptions? AzureBlob { get; set; }
    }

    public class State
    {
        public int NumberOfReleases { get; set; } = 0;
        public string[] Versions { get; set; } = [];
    }
}
