using Microsoft.Extensions.FileProviders;

namespace k8s.Frontman.Features.Providers.AzureBlob;

public class AzureBlobFileProviderOptions : IFileProviderFactory
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string? Root { get; set; }

    public IFileProvider Create()
    {
        return new AzureBlobFileProvider(ConnectionString, ContainerName, Root ?? string.Empty);
    }
}
