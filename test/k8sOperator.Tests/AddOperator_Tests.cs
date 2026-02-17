using k8s.Operator;
using k8s.Operator.Cli;
using k8s.Operator.Configuration;
using k8s.Operator.Informer;
using k8s.Operator.Reconciler;
using k8sOperator.Tests.TestKube;

namespace k8sOperator.Tests;

public class AddOperator_Tests
{
    [Test]
    public async Task Should_register_correct_services()
    {
        using var server = new TestKubeApiServer();

        var services = new ServiceCollection();

        services.AddOperator(x =>
        {
            x.Kubernetes = server.GetKubernetesClientConfiguration();
        });

        var serviceProvider = services.BuildServiceProvider();

        await Assert.That(() => serviceProvider.GetRequiredService<OperatorConfiguration>()).ThrowsNothing();
        await Assert.That(() => serviceProvider.GetRequiredService<SharedInformerFactory>()).ThrowsNothing();
        await Assert.That(() => serviceProvider.GetRequiredService<ReconcilerFactory>()).ThrowsNothing();
        await Assert.That(() => serviceProvider.GetRequiredService<CommandRegistry>()).ThrowsNothing();
    }
}
