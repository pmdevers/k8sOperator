using Microsoft.Extensions.FileProviders;

namespace k8s.Frontman.Features.Providers.AzureBlob;

public class AzureBlobFileProviderOptions : IFileProviderFactory
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string? Root { get; set; }

    public IFileProvider Create()
    {
        return new AzureBlobFileProvider(GetConnectionString(), ContainerName, Root ?? string.Empty);
    }

    public string GetConnectionString()
        => $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccountKey};EndpointSuffix=core.windows.net";


}
