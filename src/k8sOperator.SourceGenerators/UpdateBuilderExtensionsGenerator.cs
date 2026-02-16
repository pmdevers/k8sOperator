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
        // Find all classes/records that implement IKubernetesObject
        var kubernetesObjectDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateType(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(kubernetesObjectDeclarations.Collect());

        // Generate the extensions
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsCandidateType(SyntaxNode node)
    {
        return (node is ClassDeclarationSyntax || node is RecordDeclarationSyntax)
            && node is TypeDeclarationSyntax typeDeclaration
            && typeDeclaration.BaseList != null
            && typeDeclaration.BaseList.Types.Count > 0;
    }

    private static ClassInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        // Check if it implements IKubernetesObject
        if (!ImplementsInterface(typeSymbol, "IKubernetesObject", "k8s"))
            return null;

        // Check for Spec and Status properties
        var specProperty = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "Spec" && p.DeclaredAccessibility == Accessibility.Public);

        var statusProperty = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "Status" && p.DeclaredAccessibility == Accessibility.Public);

        // Skip if neither Spec nor Status exists
        if (specProperty == null && statusProperty == null)
            return null;

        return new ClassInfo(
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.Name,
            specProperty != null,
            specProperty?.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            specProperty?.Type.Name,
            statusProperty != null,
            statusProperty?.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            statusProperty?.Type.Name
        );
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string interfaceName, string namespacePrefix)
    {
        foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
        {
            if (interfaceSymbol.Name == interfaceName
                && interfaceSymbol.ContainingNamespace.ToDisplayString().StartsWith(namespacePrefix))
            {
                return true;
            }
        }

        return false;
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

        // Generate one extension type per class
        foreach (var classInfo in distinctClasses)
        {
            var source = GenerateExtensionForClass(classInfo!);
            var fileName = $"{classInfo!.ClassName}BuilderExtensions.g.cs";
            context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateExtensionForClass(ClassInfo classInfo)
    {
        // Load templates
        var extensionTemplate = TemplateReader.ReadTemplate("ImplicitExtension.template");
        var specMethodTemplate = TemplateReader.ReadTemplate("ImplicitWithSpecMethod.template");
        var statusMethodTemplate = TemplateReader.ReadTemplate("ImplicitWithStatusMethod.template");

        // Generate all methods
        var methods = new StringBuilder();

        // Generate WithSpec method if Spec property exists
        if (classInfo.HasSpec && classInfo.SpecTypeName != null)
        {
            var specMethod = specMethodTemplate
                .Replace("{{CLASS_NAME}}", classInfo.ClassName)
                .Replace("{{SPEC_TYPE_NAME}}", classInfo.SpecTypeName);

            methods.Append(specMethod);

            // Add line break if we're also adding status method
            if (classInfo.HasStatus)
            {
                methods.AppendLine();
            }
        }

        // Generate WithStatus method if Status property exists
        if (classInfo.HasStatus && classInfo.StatusTypeName != null)
        {
            var statusMethod = statusMethodTemplate
                .Replace("{{CLASS_NAME}}", classInfo.ClassName)
                .Replace("{{STATUS_TYPE_NAME}}", classInfo.StatusTypeName);

            methods.Append(statusMethod);
        }

        // Replace placeholders in extension template
        var result = extensionTemplate
            .Replace("{{NAMESPACE}}", classInfo.Namespace)
            .Replace("{{EXTENSION_NAME}}", $"{classInfo.ClassName}BuilderExtensions")
            .Replace("{{CLASS_NAME}}", classInfo.ClassName)
            .Replace("{{METHODS}}", methods.ToString());

        return result;
    }

    private record ClassInfo(
        string Namespace,
        string ClassName,
        bool HasSpec,
        string? SpecTypeFullName,
        string? SpecTypeName,
        bool HasStatus,
        string? StatusTypeFullName,
        string? StatusTypeName);
}
