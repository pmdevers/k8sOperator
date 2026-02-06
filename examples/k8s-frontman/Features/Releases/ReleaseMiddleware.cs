using k8s.Frontman.Features.Providers;
using k8s.Operator.Informer;
using Microsoft.AspNetCore.StaticFiles;

namespace k8s.Frontman.Features.Releases;

public class ReleaseMiddleware(RequestDelegate next,
    IInformer<Release> releases,
    IInformer<Provider> providers)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var segments = (context.Request.Path.Value ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var releaseName = segments.FirstOrDefault() ?? string.Empty;

        var release = releases.List()
            .FirstOrDefault(x => x.Spec.Url == releaseName);

        if (release == null)
        {
            await next(context);
            return;
        }

        var newPath = string.Join('/', ["", release.Status?.CurrentVersion, .. segments.Skip(1)]);

        var providerName = release.Spec.Provider;
        var provider = providers.Get(providerName, release.Metadata.NamespaceProperty);

        if (provider is null)
        {
            return;
        }

        var fileProvider = (provider.Spec.File?.Create() ?? provider.Spec.AzureBlob?.Create());

        if (fileProvider is null)
        {
            return;
        }

        var fileInfo = fileProvider.GetFileInfo(newPath);

        // If path is a directory or doesn't exist, try index.html
        if (!fileInfo.Exists || fileInfo.IsDirectory)
        {
            var indexPath = newPath.TrimEnd('/') + "/index.html";
            var indexFileInfo = fileProvider.GetFileInfo(indexPath);

            if (indexFileInfo.Exists && !indexFileInfo.IsDirectory)
            {
                fileInfo = indexFileInfo;
            }
            else if (!fileInfo.Exists)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("File not found");
                return;
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("File not found");
                return;
            }
        }

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        if (!contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        context.Response.ContentType = contentType;
        context.Response.ContentLength = fileInfo.Length;

        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        context.Response.Headers.ETag = $"\"{release.Status?.CurrentVersion}-{fileInfo.LastModified.Ticks}\"";

        using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body);
    }
}
