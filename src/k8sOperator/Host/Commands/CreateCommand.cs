using k8s.Operator;
using k8s.Operator.Configuration;
using k8s.Operator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static k8s.Operator.Helpers.ConsoleHelpers;

namespace k8s.Operator.Host.Commands;

[OperatorArgument(
    Command = "create",
    Description = "Creates a resource definition.",
    Aliases = new[] { "-c", "--create" },
    Order = 4)]
public class CreateCommand(IHost app) : IOperatorCommand
{
    public Task RunAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine($"{RED}Please provide a resourcename.{NORMAL}");
            return Task.CompletedTask;
        }

        var config = app.Services.GetRequiredService<OperatorConfiguration>();
        var datasource = app.Services.GetRequiredService<ControllerDatasource>();
        var watchers = datasource.GetControllers().ToList();
        var controller = watchers.FirstOrDefault(x => x.ResourceType.Name.Equals(args[1], StringComparison.CurrentCultureIgnoreCase));

        if (controller == null)
        {
            Console.WriteLine($"{RED}Unknown resource: {args[1]}{NORMAL}");
            return Task.CompletedTask;
        }

        var activator = Activator.CreateInstance(controller.ResourceType) as CustomResource;
        activator.Initialize();

        activator!.Metadata = new()
        {
            Name = args[1],
            NamespaceProperty = config.Namespace
        };

        Console.WriteLine(KubernetesYaml.Serialize(activator));
        return Task.CompletedTask;
    }
}
