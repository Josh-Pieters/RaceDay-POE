namespace RaceDay.Api.Services;

/// <summary>Checks that an uploaded file really is a small JPG, PNG or WebP image.</summary>
public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    /// <summary>Returns an error message (or null if valid) and the file extension to use.</summary>
    public static async Task<(string? Error, string Extension)> ValidateAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return ("Please choose an image file.", "");
        if (file.Length > MaxBytes)
            return ("The image must be 5 MB or smaller.", "");
        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
            return ("Only JPG, PNG or WebP images are allowed.", "");

        // Check the file's first bytes so a renamed text file cannot pass as an image.
        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAsync(header, 0, header.Length);
        }

        var looksValid = extension switch
        {
            ".jpg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                       && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P',
            _ => false
        };

        return looksValid ? (null, extension) : ("The file does not look like a valid image.", "");
    }
}