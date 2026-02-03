using Microsoft.Extensions.FileProviders;

namespace k8s.Frontman.Features.Providers.Azure;

public class AzureBlobFileProviderOptions : IFileProviderFactory
{
    public string AccountName { get; set; }
    public string AccountKey { get; set; }
    public string ContainerName { get; set; }
    public string? Root { get; set; }

    public IFileProvider Create()
    {
        return new AzureBlobFileProvider(GetConnectionString(), ContainerName, Root ?? string.Empty);
    }

    public string GetConnectionString()
        => $"DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccountKey};EndpointSuffix=core.windows.net";

    
}
