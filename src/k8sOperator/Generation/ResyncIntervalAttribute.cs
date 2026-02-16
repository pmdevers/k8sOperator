namespace k8s.Operator.Generation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ResyncIntervalAttribute() : PatternAttribute("^([0-9]+(\\.[0-9]+)?(ms|s|m|h))+$")
{
}

