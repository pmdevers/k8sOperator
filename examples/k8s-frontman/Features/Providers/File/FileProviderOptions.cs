using Microsoft.Extensions.FileProviders;

namespace k8s.Frontman.Features.Providers.File;

public class FileProviderOptions : IFileProviderFactory
{
    public string Path { get; set; } = string.Empty;

    public IFileProvider Create()
        => new PhysicalFileProvider(Path);
}
