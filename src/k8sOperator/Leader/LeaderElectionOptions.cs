namespace k8s.Operator.Leader;

public class LeaderElectionOptions
{
    public string LeaseName { get; set; } = string.Empty;
    public string LeaseNamespace { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan RenewInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RetryPeriod { get; set; } = TimeSpan.FromSeconds(2);
}
