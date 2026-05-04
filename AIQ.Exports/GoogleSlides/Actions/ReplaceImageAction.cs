using Google.Apis.Drive.v3;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
/// <summary>
/// Replaces image placeholders in Google Slides with an image from a URL or local file path.
/// </summary>
public class ReplaceImageAction : IGoogleSlidesAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplaceImageAction"/> class.
    /// </summary>
    /// <param name="slideNumber">The slide number (1-based, null for all slides).</param>
    /// <param name="imageElementName">The name of the image element to replace.</param>
    /// <param name="imagePath">The image path, either a public URL or a local file path.</param>
    public ReplaceImageAction(int? slideNumber, string imageElementName, string imagePath)
    {
        SlideNumber = slideNumber;
        ImageElementName = imageElementName;
        ImagePath = imagePath;
        _publicImageUrl = imagePath.StartsWith("http://") || imagePath.StartsWith("https://") ? imagePath : null;
    }

    public int? SlideNumber { get; }
    public string ImageElementName { get; }
    public string ImagePath { get; }

    private string? _publicImageUrl;
    private string? _driveFileId;

    /// <summary>
    /// Asynchronously uploads local image material to Google Drive if the image path is a local file.
    /// </summary>
    /// <param name="driveService">The Google Drive service instance to use for upload.</param>
    public async Task UploadRequestMaterialAsync(DriveService driveService)
    {
        if (ImagePath.StartsWith("http://") || ImagePath.StartsWith("https://"))
        {
            return;
        }

        var fileInfo = new FileInfo(ImagePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Image file not found: {ImagePath}");
        }

        var mimeType = fileInfo.Extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "image/png"
        };

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileInfo.Name,
            MimeType = mimeType
        };

        using var fileStream = fileInfo.OpenRead();
        var uploadRequest = driveService.Files.Create(fileMetadata, fileStream, mimeType);
        uploadRequest.Fields = "id";
        var uploadResult = await uploadRequest.UploadAsync();

        if (uploadResult.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            throw new Exception($"Failed to upload image to Google Drive: {uploadResult.Exception?.Message}");
        }

        _driveFileId = uploadRequest.ResponseBody.Id;

        var permission = new Google.Apis.Drive.v3.Data.Permission
        {
            Type = "anyone",
            Role = "reader"
        };
        await driveService.Permissions.Create(permission, _driveFileId).ExecuteAsync();

        _publicImageUrl = $"https://drive.google.com/uc?export=view&id={_driveFileId}";
    }

    /// <summary>
    /// Asynchronously deletes the uploaded image from Google Drive if it was uploaded.
    /// </summary>
    /// <param name="driveService">The Google Drive service instance to use for deletion.</param>
    public async Task DeleteRequestMaterialAsync(DriveService driveService)
    {
        if (string.IsNullOrEmpty(_driveFileId))
        {
            return;
        }

        try
        {
            await driveService.Files.Delete(_driveFileId).ExecuteAsync();
        }
        catch
        {
            // Suppress cleanup errors
        }
    }

    public Request ToRequest(Presentation presentation)
    {
        if (string.IsNullOrEmpty(_publicImageUrl))
        {
            throw new InvalidOperationException("Image URL is not available. Ensure UploadRequestMaterialAsync has been called for local image paths.");
        }

        var slideNumberToId = presentation.Slides?
            .Select((slide, i) => new { slide.ObjectId, Index = i + 1 })
            .ToDictionary(x => x.Index, x => x.ObjectId) ?? [];

        string[]? pageObjectIds = SlideNumber.HasValue &&
                                  slideNumberToId.TryGetValue(SlideNumber.Value, out var id)
                                      ? [id]
                                      : null;

        var replaceRequest = new ReplaceAllShapesWithImageRequest
        {
            ContainsText = new SubstringMatchCriteria
            {
                Text = ImageElementName,
                MatchCase = false
            },
            ImageUrl = _publicImageUrl,

            PageObjectIds = pageObjectIds
        };

        return new Request { ReplaceAllShapesWithImage = replaceRequest };
    }
}