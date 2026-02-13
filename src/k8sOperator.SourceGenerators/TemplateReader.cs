using System;
using System.IO;
using System.Reflection;

namespace k8s.Operator.SourceGenerators;

internal static class TemplateReader
{
    public static string ReadTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"k8s.Operator.SourceGenerators.Templates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
