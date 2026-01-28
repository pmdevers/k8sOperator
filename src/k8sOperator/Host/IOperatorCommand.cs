namespace k8s.Operator.Host;

public interface IOperatorCommand
{
    Task RunAsync(string[] args);
}
