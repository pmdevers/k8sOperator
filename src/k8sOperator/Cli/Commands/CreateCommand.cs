using k8s.Models;
using k8s.Operator.Configuration;
using System.Reflection;
using static k8s.Operator.Cli.Helpers.ConsoleHelpers;


namespace k8s.Operator.Cli.Commands;

[OperatorCommand("create", "Creates a resource definition.", 4, "-c", "--create")]
public class CreateCommand(OperatorConfiguration config) : IOperatorCommand
{
    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine($"{RED}Please provide a resourcename.{NORMAL}");
            return 1;
        }

        var resource = config.Install.Resources
            .FirstOrDefault(t => t.GetCustomAttribute<KubernetesEntityAttribute>()?.Kind.Equals(args[0], StringComparison.CurrentCultureIgnoreCase) == true);

        if (resource == null)
        {
            Console.WriteLine($"{RED}Unknown resource: {args[1]}{NORMAL}");
            return 1;
        }

        var activator = Activator.CreateInstance(resource) as IKubernetesObject<V1ObjectMeta>;
        activator.Initialize();

        activator!.Metadata = new()
        {
            Name = args[0],
            NamespaceProperty = config.Namespace
        };

        Console.WriteLine(KubernetesYaml.Serialize(activator));
        return 0;
    }
}
