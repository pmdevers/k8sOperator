namespace Simplicity.Operator.Configuration;

public record OperatorConfiguration
{
    public string Name { get; set; } = "my-operator";
    public string Version { get; set; } = "0.1.0";
    public string Namespace { get; set; } = "default";
    public LeaseConfiguration Lease { get; set; } = new LeaseConfiguration();
}

public class LeaseConfiguration
{
    public string LeaseName { get; set; } = "my-operator-leader-election";
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RenewDeadline { get; set; } = TimeSpan.FromSeconds(20);
    public TimeSpan RetryPeriod { get; set; } = TimeSpan.FromSeconds(10);
}
