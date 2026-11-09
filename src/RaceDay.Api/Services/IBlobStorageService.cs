namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    /// <summary>Uploads an image to blob storage and returns its public URL.</summary>
    /// <param name="content">The image bytes.</param>
    /// <param name="folder">Virtual folder inside the container, e.g. "banners" or "profiles".</param>
    /// <param name="extension">File extension including the dot, e.g. ".png".</param>
    /// <param name="contentType">MIME type, e.g. "image/png".</param>
    Task<string> UploadImageAsync(Stream content, string folder, string extension, string contentType,
        CancellationToken cancellationToken = default);
}

/// <summary>Thrown when Azure Blob Storage is not configured or cannot be reached.</summary>
public class BlobStorageUnavailableException : Exception
{
    public BlobStorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}