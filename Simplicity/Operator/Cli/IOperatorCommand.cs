namespace Simplicity.Operator.Cli;

public interface IOperatorCommand
{
    Task ExecuteAsync(string[] args);
}
