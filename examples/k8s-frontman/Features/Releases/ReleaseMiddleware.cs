using k8s.Frontman.Features.Providers;
using k8s.Operator.Informer;
using Microsoft.AspNetCore.StaticFiles;

namespace k8s.Frontman.Features.Releases;

public class ReleaseMiddleware(RequestDelegate next,
    IInformer<V1Release> releases, IInformer<V1Provider> providers)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string url = context.Request.Path.Value ?? string.Empty;

        var release = FindRelease(url);

        if (release == null)
        {
            await next(context);
            return;
        }

        var remainder = url[release.Spec.Url.Length..];
        var newPath = string.Join('/', ["", release.Status?.CurrentVersion, remainder]);

        var provider = providers.Indexer.Get(release.Spec.Provider, release.Metadata.NamespaceProperty);

        if (provider is null)
        {
            return;
        }

        var fileProvider = provider.GetFileProvider();

        if (fileProvider is null)
        {
            return;
        }

        var fileInfo = fileProvider.GetFileInfo(newPath);

        // If path is a directory or doesn't exist, try index.html
        if (!fileInfo.Exists || fileInfo.IsDirectory)
        {
            var indexPath = string.Join('/', ["", release.Status?.CurrentVersion, "index.html"]);
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

    private V1Release? FindRelease(string urlPath)
    {
        var segments = urlPath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        while (segments.Count > 0)
        {
            var currentPath = "/" + string.Join('/', segments);

            var match = releases.List().FirstOrDefault(release =>
                release.Spec.Url.Equals(currentPath, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                return match;
            }

            // Remove last segment and try again
            segments.RemoveAt(segments.Count - 1);
        }

        // Try root path as final fallback
        return releases.List().FirstOrDefault(release =>
            release.Spec.Url.Equals("/", StringComparison.OrdinalIgnoreCase));
    }
}
