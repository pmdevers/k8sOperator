using System;
using System.IO;

namespace k8sOperator.BuildTasks;

internal static class Reader
{
    internal static string ReadEmbeddedResource(string resourceName)
    {
        var type = typeof(Reader);
        var assembly = type.Assembly;
        var fullResourceName = $"{type.Namespace}.Templates.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {fullResourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
