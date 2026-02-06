using k8s.Operator;
using System.Text.RegularExpressions;

namespace k8s.Frontman.Features.Providers;

public static partial class ProviderReconciler
{
    public static async Task ReconcileAsync(OperatorContext context)
    {
        var informer = context.GetInformer<Provider>();
        var key = context.ResourceKey;

        var provider = context.Resource as Provider;
        if (provider == null)
        {
            return;
        }

        await context.Update<Provider>()
            .AddLabel("managed-by", "simple-operator")
            .AddLabel("processed", "true")
            .AddAnnotation("last-reconcile", DateTime.UtcNow.ToString("o"))
            .ApplyAsync();

        var fileprovider = (provider.Spec.File?.Create() ?? provider.Spec.AzureBlob?.Create());

        if (fileprovider is not null)
        {
            var dirs = fileprovider.GetDirectoryContents("")
                .Where(x => x.IsDirectory)
                .Select(x => x.Name).ToList();

            await context.Update<Provider>()
            .WithStatus(x =>
            {
                x.Status ??= new Provider.State();
                x.Status.NumberOfReleases = dirs.Count;
                x.Status.Versions = dirs.TakeLast(10).ToArray();
            })
            .ApplyAsync();
        }

        var timeSpan = ParseCustomTime(provider.Spec.Interval);

        await context.Queue.Requeue(key, timeSpan, context.CancellationToken);
    }

    static TimeSpan ParseCustomTime(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.");

        int hours = 0, minutes = 0, seconds = 0;

        // Regex to match number + unit (h, m, s)
        var matches = Timespan().Matches(input);

        foreach (Match match in matches)
        {
            int value = int.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value.ToLower();

            switch (unit)
            {
                case "h": hours += value; break;
                case "m": minutes += value; break;
                case "s": seconds += value; break;
                default: throw new FormatException($"Unknown unit: {unit}");
            }
        }

        return new TimeSpan(hours, minutes, seconds);
    }

    [GeneratedRegex(@"(\d+)\s*([hms])", RegexOptions.IgnoreCase)]
    private static partial Regex Timespan();
}
