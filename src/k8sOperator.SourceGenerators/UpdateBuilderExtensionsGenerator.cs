using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace k8s.Operator.SourceGenerators;

[Generator]
public class UpdateBuilderExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes that inherit from CustomResource<TSpec, TStatus>
        var customResourceDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateClass(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(customResourceDeclarations.Collect());

        // Generate the extensions
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration
            && classDeclaration.BaseList != null
            && classDeclaration.BaseList.Types.Count > 0;
    }

    private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            return null;

        // Check if it inherits from CustomResource<TSpec, TStatus>
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "CustomResource"
                && baseType.ContainingNamespace.ToDisplayString() == "k8s.Operator.Models"
                && baseType.TypeArguments.Length == 2)
            {
                var specType = baseType.TypeArguments[0];
                var statusType = baseType.TypeArguments[1];

                return new ClassInfo(
                    classSymbol.ContainingNamespace.ToDisplayString(),
                    classSymbol.Name,
                    specType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    statusType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    specType.Name,
                    statusType.Name
                );
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    private static void Execute(
        Compilation compilation,
        ImmutableArray<ClassInfo?> classes,
        SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var distinctClasses = classes
            .Where(c => c is not null)
            .Distinct()
            .ToList();

        if (!distinctClasses.Any())
            return;

        // Group by namespace
        var grouped = distinctClasses.GroupBy(c => c!.Namespace);

        foreach (var group in grouped)
        {
            var source = GenerateExtensionsForNamespace(group.Key!, group.ToList()!);
            context.AddSource($"UpdateBuilderExtensions.{group.Key}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateExtensionsForNamespace(string namespaceName, List<ClassInfo> classes)
    {
        // Load templates
        var classTemplate = TemplateReader.ReadTemplate("UpdateBuilderExtensions.template");
        var specMethodTemplate = TemplateReader.ReadTemplate("WithSpecMethod.template");
        var statusMethodTemplate = TemplateReader.ReadTemplate("WithStatusMethod.template");

        // Generate all methods
        var methods = new StringBuilder();
        for (int i = 0; i < classes.Count; i++)
        {
            var classInfo = classes[i];

            // Generate WithSpec method
            var specMethod = specMethodTemplate
                .Replace("{{CLASS_NAME}}", classInfo.ClassName)
                .Replace("{{SPEC_TYPE_NAME}}", classInfo.SpecTypeName);

            methods.Append(specMethod);
            methods.AppendLine();

            // Generate WithStatus method
            var statusMethod = statusMethodTemplate
                .Replace("{{CLASS_NAME}}", classInfo.ClassName)
                .Replace("{{STATUS_TYPE_NAME}}", classInfo.StatusTypeName);

            methods.Append(statusMethod);

            // Add separator between classes (but not after the last one)
            if (i < classes.Count - 1)
            {
                methods.AppendLine();
            }
        }

        // Replace placeholders in class template
        var result = classTemplate
            .Replace("{{NAMESPACE}}", namespaceName)
            .Replace("{{METHODS}}", methods.ToString());

        return result;
    }

    private record ClassInfo(
        string Namespace,
        string ClassName,
        string SpecTypeFullName,
        string StatusTypeFullName,
        string SpecTypeName,
        string StatusTypeName);
}
