using k8s.Operator.Models;

namespace simple_operator.Features.ManageApplication;
/// <summary>
/// Example custom resource
/// </summary>
[k8s.Models.KubernetesEntity(Group = "example.io", ApiVersion = "v1", Kind = "MyApp", PluralName = "myapps")]
public class MyApp : CustomResource<MyApp.MyAppSpec, MyApp.MyAppStatus>
{
    public class MyAppSpec
    {
        public int Replicas { get; set; }
        public string? Image { get; set; }
    }

    public class MyAppStatus
    {
        public string? Phase { get; set; }
        public int ReadyReplicas { get; set; }
    }

}
