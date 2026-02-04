using k8s.Models;
using k8s.Operator.Metadata;
using k8s.Operator.Models;

namespace k8s.Frontman.Features.Releases;

[KubernetesEntity(Group = "frontman.io", ApiVersion = "v1alpha1", Kind = "Release", PluralName = "releases")]
[AdditionalPrinterColumn(Name = "Url", Description = "", Path = ".spec.url", Type = "string")]
[AdditionalPrinterColumn(Name = "Version", Description = "", Path = ".spec.version", Type = "string")]
[AdditionalPrinterColumn(Name = "Previous", Description = "", Path = ".status.previousVersion", Type = "string")]
[AdditionalPrinterColumn(Name = "Message", Description = "", Path = ".status.message", Type = "string")]
public class Release : CustomResource<Release.Specs, Release.State>
{
    public class Specs
    {
        public string Provider { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public class State
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string PreviousVersion { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
