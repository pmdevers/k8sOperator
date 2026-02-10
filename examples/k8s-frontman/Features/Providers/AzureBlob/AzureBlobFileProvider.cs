using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using k8s.Frontman.Features.Providers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace k8s.Frontman.Features.Providers.AzureBlob;

public class AzureBlobFileProvider(string connectionString, string containerName, string root) : IFileProvider
{
    private readonly BlobContainerClient _containerClient = new(connectionString, containerName);
    private readonly string _basePath = root.TrimStart('/').TrimEnd('/');

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var normalizedPath = NormalizePath(subpath);
        return new AzureBlobDirectoryContents(_containerClient, normalizedPath);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var normalizedPath = NormalizePath(subpath);
        var blobClient = _containerClient.GetBlobClient(normalizedPath);
        return new AzureBlobFileInfo(blobClient, normalizedPath);
    }

    public IChangeToken Watch(string filter)
        => NullChangeToken.Singleton;

    private string NormalizePath(string subpath)
    {
        var normalized = subpath.TrimStart('/');

        if (!string.IsNullOrEmpty(_basePath))
        {
            normalized = string.IsNullOrEmpty(normalized)
                ? _basePath
                : $"{_basePath}/{normalized}";
        }

        return normalized;
    }

    internal class AzureBlobDirectoryContents(BlobContainerClient containerClient, string prefix) : IDirectoryContents
    {
        private readonly string _prefix = prefix.TrimEnd('/');

        public bool Exists
        {
            get
            {
                try
                {
                    var options = new GetBlobsOptions() { Prefix = _prefix };
                    var blobs = containerClient.GetBlobs(options).Take(1);
                    return blobs.Any();
                }
                catch
                {
                    return false;
                }
            }
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            var prefix = string.IsNullOrEmpty(_prefix) ? "" : $"{_prefix}/";
            var options = new GetBlobsOptions() { Prefix = prefix };

            var blobs = containerClient.GetBlobs(options);

            foreach (var blobItem in blobs)
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                yield return new AzureBlobFileInfo(blobClient, blobItem.Name, true);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    internal class AzureBlobFileInfo(BlobClient blobClient, string name, bool isDirectory = false) : IFileInfo
    {
        private readonly string _name = (isDirectory ? Path.GetDirectoryName(name) : Path.GetFileName(name)) ?? string.Empty;
        private BlobProperties? _properties;

        public bool Exists
        {
            get
            {
                try
                {
                    EnsureProperties();
                    return _properties != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        public long Length
        {
            get
            {
                EnsureProperties();
                return _properties?.ContentLength ?? -1;
            }
        }

        public string? PhysicalPath => null;

        public string Name => _name;

        public DateTimeOffset LastModified
        {
            get
            {
                EnsureProperties();
                return _properties?.LastModified ?? DateTimeOffset.MinValue;
            }
        }

        public bool IsDirectory => isDirectory;

        public Stream CreateReadStream()
        {
            if (!Exists)
            {
                throw new FileNotFoundException($"Blob '{blobClient.Name}' not found.");
            }

            return blobClient.OpenRead();
        }

        private void EnsureProperties()
        {
            if (_properties == null)
            {
                try
                {
                    _properties = blobClient.GetProperties().Value;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _properties = null;
                }
            }
        }
    }
}
