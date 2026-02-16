using k8s.Frontman.Features.Providers;
using k8s.Frontman.Features.Releases;
using System.Text.Json.Serialization;

namespace k8s.Frontman;

[JsonSerializable(typeof(GetReleases.ReleaseResponse))]
[JsonSerializable(typeof(GetReleases.ReleaseResponse[]))]
[JsonSerializable(typeof(V1Provider))]
[JsonSerializable(typeof(V1Release))]
[JsonSerializable(typeof(V1ProviderSpec))]
[JsonSerializable(typeof(V1ReleaseSpec))]
[JsonSerializable(typeof(V1ProviderStatus))]
[JsonSerializable(typeof(V1ReleaseStatus))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
