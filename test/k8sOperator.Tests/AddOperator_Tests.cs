using k8s;
using k8s.Operator;
using k8s.Operator.Informer;
using k8s.Operator.Models;
using k8s.Operator.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace k8sOperator.Tests;


public class AddOperator_Tests
{
    [Test]
    public async Task Should_register_correct_services()
    {
        var services = new ServiceCollection();
        services.AddOperator(x =>
        {
            x.Operator.Name = "test-operator";
            x.Operator.Namespace = "default";
            x.Operator.ContainerRepository = "test-repo";
        });
        var serviceProvider = services.BuildServiceProvider();

        var datasource = serviceProvider.GetService<ControllerDatasource>();
        var informerFactory = serviceProvider.GetService<IInformerFactory>();
        var workQueueType = serviceProvider.GetService(typeof(IWorkQueue<ResourceKey>));
        var kubernetesClient = serviceProvider.GetService<IKubernetes>();

        await Assert.That(informerFactory).IsNotNull();
        await Assert.That(workQueueType).IsNotNull();
        await Assert.That(datasource).IsNotNull();
        await Assert.That(kubernetesClient).IsNotNull();
    }
}
