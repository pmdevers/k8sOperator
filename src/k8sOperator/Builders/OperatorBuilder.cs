using k8s.Operator.Configuration;
using k8s.Operator.Leader;

namespace k8s.Operator.Builders;

public class OperatorBuilder
{
    public KubernetesClientConfiguration? Configuration { get; init; }
    public LeaderElectionOptions LeaderElection { get; init; }
        = new LeaderElectionOptions();
    public OperatorConfiguration Operator { get; init; }
        = new OperatorConfiguration();
}
