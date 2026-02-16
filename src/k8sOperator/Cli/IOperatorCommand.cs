namespace k8s.Operator.Cli;

public interface IOperatorCommand
{
    Task ExecuteAsync(string[] args);
}
