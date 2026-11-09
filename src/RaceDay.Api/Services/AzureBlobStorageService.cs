using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Stores images in an Azure Blob Storage container. Settings come from configuration:
///   AzureStorage:ConnectionString   (keep this in user secrets, never in Git)
///   AzureStorage:ContainerName      (defaults to "raceday-images")
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _container;

    public AzureBlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"];
        var containerName = config["AzureStorage:ContainerName"] ?? "raceday-images";

        // A missing or invalid setting must not stop the rest of the API from working.
        // Uploads simply report "unavailable" until the setting is fixed.
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                _container = new BlobContainerClient(connectionString, containerName);
            }
            catch (FormatException)
            {
                _container = null;
            }
        }
    }

    public async Task<string> UploadImageAsync(Stream content, string folder, string extension, string contentType,
        CancellationToken cancellationToken = default)
    {
        if (_container == null)
            throw new BlobStorageUnavailableException("Azure Blob Storage is not configured.");

        try
        {
            // Blob-level public access lets browsers display the image straight from its URL.
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");
            await blob.UploadAsync(content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                cancellationToken);

            return blob.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            throw new BlobStorageUnavailableException("Could not upload the image to Azure Blob Storage.", ex);
        }
    }
}