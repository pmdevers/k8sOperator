using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace k8s.Operator.SourceGenerators;

//[Generator]
public class KubernetesBuiltInResourcesGenerator : IIncrementalGenerator
{
    // Define all Kubernetes resources that have Spec/Status
    private static readonly ImmutableArray<ResourceInfo> KubernetesResources = ImmutableArray.Create(
        // Core Resources
        new ResourceInfo("V1Pod", "V1PodSpec", "V1PodStatus", true, true),
        new ResourceInfo("V1Service", "V1ServiceSpec", "V1ServiceStatus", true, true),
        new ResourceInfo("V1ConfigMap", null, null, false, false, hasDataMethods: true),
        new ResourceInfo("V1Secret", null, null, false, false, hasSecretMethods: true),
        new ResourceInfo("V1Namespace", "V1NamespaceSpec", "V1NamespaceStatus", true, true),
        new ResourceInfo("V1Node", "V1NodeSpec", "V1NodeStatus", true, true),
        new ResourceInfo("V1PersistentVolume", "V1PersistentVolumeSpec", "V1PersistentVolumeStatus", true, true),
        new ResourceInfo("V1PersistentVolumeClaim", "V1PersistentVolumeClaimSpec", "V1PersistentVolumeClaimStatus", true, true),
        new ResourceInfo("V1ServiceAccount", null, null, false, false, hasServiceAccountMethods: true),

        // Apps Resources
        new ResourceInfo("V1Deployment", "V1DeploymentSpec", "V1DeploymentStatus", true, true),
        new ResourceInfo("V1StatefulSet", "V1StatefulSetSpec", "V1StatefulSetStatus", true, true),
        new ResourceInfo("V1DaemonSet", "V1DaemonSetSpec", "V1DaemonSetStatus", true, true),
        new ResourceInfo("V1ReplicaSet", "V1ReplicaSetSpec", "V1ReplicaSetStatus", true, true),

        // Batch Resources
        new ResourceInfo("V1Job", "V1JobSpec", "V1JobStatus", true, true),
        new ResourceInfo("V1CronJob", "V1CronJobSpec", "V1CronJobStatus", true, true),

        // Networking Resources
        new ResourceInfo("V1Ingress", "V1IngressSpec", "V1IngressStatus", true, true),
        new ResourceInfo("V1NetworkPolicy", "V1NetworkPolicySpec", null, true, false),

        // RBAC Resources
        new ResourceInfo("V1Role", null, null, false, false, hasRbacMethods: true),
        new ResourceInfo("V1ClusterRole", null, null, false, false, hasRbacMethods: true),
        new ResourceInfo("V1RoleBinding", null, null, false, false, hasRoleBindingMethods: true),
        new ResourceInfo("V1ClusterRoleBinding", null, null, false, false, hasRoleBindingMethods: true),

        // Storage Resources
        new ResourceInfo("V1StorageClass", null, null, false, false, hasStorageClassMethods: true),
        new ResourceInfo("V1VolumeAttachment", "V1VolumeAttachmentSpec", "V1VolumeAttachmentStatus", true, true),

        // Policy Resources
        new ResourceInfo("V1PodDisruptionBudget", "V1PodDisruptionBudgetSpec", "V1PodDisruptionBudgetStatus", true, true),

        // Autoscaling Resources
        new ResourceInfo("V1HorizontalPodAutoscaler", "V1HorizontalPodAutoscalerSpec", "V1HorizontalPodAutoscalerStatus", true, true),
        new ResourceInfo("V2HorizontalPodAutoscaler", "V2HorizontalPodAutoscalerSpec", "V2HorizontalPodAutoscalerStatus", true, true),

        // Coordination Resources
        new ResourceInfo("V1Lease", "V1LeaseSpec", null, true, false)
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => GenerateExtensions(ctx));
    }

    private static void GenerateExtensions(IncrementalGeneratorPostInitializationContext context)
    {
        // Load templates
        var classTemplate = TemplateReader.ReadTemplate("KubernetesResourceBuilderExtensions.template");
        var extensionBlockTemplate = TemplateReader.ReadTemplate("K8sExtensionBlock.template");
        var withSpecTemplate = TemplateReader.ReadTemplate("K8sWithSpecMethod.template");
        var withStatusTemplate = TemplateReader.ReadTemplate("K8sWithStatusMethod.template");
        var configMapTemplate = TemplateReader.ReadTemplate("K8sConfigMapMethods.template");
        var secretTemplate = TemplateReader.ReadTemplate("K8sSecretMethods.template");
        var serviceAccountTemplate = TemplateReader.ReadTemplate("K8sServiceAccountMethods.template");
        var rbacTemplate = TemplateReader.ReadTemplate("K8sRbacMethods.template");
        var roleBindingTemplate = TemplateReader.ReadTemplate("K8sRoleBindingMethods.template");
        var storageClassTemplate = TemplateReader.ReadTemplate("K8sStorageClassMethods.template");

        var allExtensions = new StringBuilder();

        // Generate all resource extensions
        foreach (var resource in KubernetesResources)
        {
            var extensionMethods = new StringBuilder();

            // Generate Spec method
            if (resource.HasSpec && !string.IsNullOrEmpty(resource.SpecType))
            {
                var specMethod = withSpecTemplate.Replace("{{SPEC_TYPE}}", resource.SpecType);
                extensionMethods.AppendLine(specMethod);
            }

            // Generate Status method
            if (resource.HasStatus && !string.IsNullOrEmpty(resource.StatusType))
            {
                var statusMethod = withStatusTemplate.Replace("{{STATUS_TYPE}}", resource.StatusType);
                extensionMethods.AppendLine(statusMethod);
            }

            // Generate special methods
            if (resource.HasDataMethods)
            {
                extensionMethods.AppendLine(configMapTemplate);
            }

            if (resource.HasSecretMethods)
            {
                extensionMethods.AppendLine(secretTemplate);
            }

            if (resource.HasServiceAccountMethods)
            {
                extensionMethods.AppendLine(serviceAccountTemplate);
            }

            if (resource.HasRbacMethods)
            {
                extensionMethods.AppendLine(rbacTemplate);
            }

            if (resource.HasRoleBindingMethods)
            {
                extensionMethods.AppendLine(roleBindingTemplate);
            }

            if (resource.HasStorageClassMethods)
            {
                extensionMethods.AppendLine(storageClassTemplate);
            }

            // Build the extension block
            var extensionBlock = extensionBlockTemplate
                .Replace("{{RESOURCE_TYPE}}", resource.ResourceType)
                .Replace("{{EXTENSION_METHODS}}", extensionMethods.ToString().TrimEnd());

            allExtensions.AppendLine(extensionBlock);
            allExtensions.AppendLine();
        }

        // Generate final file
        var result = classTemplate.Replace("{{METHODS}}", allExtensions.ToString().TrimEnd());

        context.AddSource("KubernetesResourceBuilderExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private class ResourceInfo
    {
        public string ResourceType { get; }
        public string SpecType { get; }
        public string StatusType { get; }
        public bool HasSpec { get; }
        public bool HasStatus { get; }
        public bool HasDataMethods { get; }
        public bool HasSecretMethods { get; }
        public bool HasServiceAccountMethods { get; }
        public bool HasRbacMethods { get; }
        public bool HasRoleBindingMethods { get; }
        public bool HasStorageClassMethods { get; }

        public ResourceInfo(
            string resourceType,
            string specType,
            string statusType,
            bool hasSpec,
            bool hasStatus,
            bool hasDataMethods = false,
            bool hasSecretMethods = false,
            bool hasServiceAccountMethods = false,
            bool hasRbacMethods = false,
            bool hasRoleBindingMethods = false,
            bool hasStorageClassMethods = false)
        {
            ResourceType = resourceType;
            SpecType = specType;
            StatusType = statusType;
            HasSpec = hasSpec;
            HasStatus = hasStatus;
            HasDataMethods = hasDataMethods;
            HasSecretMethods = hasSecretMethods;
            HasServiceAccountMethods = hasServiceAccountMethods;
            HasRbacMethods = hasRbacMethods;
            HasRoleBindingMethods = hasRoleBindingMethods;
            HasStorageClassMethods = hasStorageClassMethods;
        }
    }
}
