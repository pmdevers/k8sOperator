namespace k8s.Operator.Metadata;

internal static class DebuggerHelpers
{
    public static string GetDebugText(string key, object value)
        => $"'{key}' - {value}";

}
