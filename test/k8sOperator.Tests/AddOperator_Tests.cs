using k8s.Operator;
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
            x.Name = "test-operator";
            x.Namespace = "default";
            x.Container.Repository = "test-repo";
        });
        var serviceProvider = services.BuildServiceProvider();
    }
}
