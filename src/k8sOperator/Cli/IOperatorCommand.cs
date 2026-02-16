namespace k8s.Operator.Cli;

public interface IOperatorCommand
{
    Task<int> ExecuteAsync(string[] args);
}
