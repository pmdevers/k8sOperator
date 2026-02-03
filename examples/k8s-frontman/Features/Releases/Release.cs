using k8s.Models;
using k8s.Operator.Models;

namespace k8s.Frontman.Features.Releases;

[KubernetesEntity(Group = "frontman.io", ApiVersion = "v1alpha", Kind = "Release", PluralName = "releases")]
public class Release : CustomResource<Release.Specs, Release.Status>
{
    public class Specs
    {
        public string Provider { get; set; }
        public string Url { get; set;  }
        public string Version { get; set; }
    }

    public class Status
    {
    }
}
