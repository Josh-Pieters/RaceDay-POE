using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Replaces Azure Blob Storage in tests: nothing is uploaded, a fake URL is returned.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(Stream content, string folder, string extension, string contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://fake.blob.core.windows.net/raceday-images/{folder}/{Guid.NewGuid():N}{extension}");
    }
}