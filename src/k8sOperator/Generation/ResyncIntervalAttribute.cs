using System.Text.RegularExpressions;

namespace k8s.Operator.Generation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public partial class ResyncIntervalAttribute() : PatternAttribute(Resync().ToString())
{
    public static TimeSpan ParseDuration(string duration)
    {
        // Parse durations like "5m", "30s", "1h", "2h30m"
        var regex = Resync();
        var matches = regex.Matches(duration);

        if (matches.Count == 0)
        {
            return TimeSpan.FromMinutes(5); // Default fallback
        }

        var totalMilliseconds = 0.0;
        foreach (var (value, unit) in from Match match in matches
                                      let value = double.Parse(match.Groups[1].Value)
                                      let unit = match.Groups[2].Value
                                      select (value, unit))
        {
            totalMilliseconds += unit switch
            {
                "ms" => value,
                "s" => value * 1000,
                "m" => value * 60 * 1000,
                "h" => value * 60 * 60 * 1000,
                _ => 0
            };
        }

        return TimeSpan.FromMilliseconds(totalMilliseconds);
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)(ms|s|m|h)")]
    private static partial Regex Resync();
}

