using System.Collections.Concurrent;

namespace HospitalManagement.BuildingBlocks.Storage;

public sealed class InMemoryBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> _store = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentNullException.ThrowIfNull(content);

        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        var key = $"{containerName}/{blobName}";
        _store[key] = (bytes, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        return key;
    }

    public Task<Stream?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var key = $"{containerName}/{blobName}";
        if (_store.TryGetValue(key, out var entry))
        {
            Stream stream = new MemoryStream(entry.Data);
            return Task.FromResult<Stream?>(stream);
        }

        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var key = $"{containerName}/{blobName}";
        return Task.FromResult(_store.TryRemove(key, out _));
    }

    public Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var key = $"{containerName}/{blobName}";
        return Task.FromResult(_store.ContainsKey(key));
    }
}
