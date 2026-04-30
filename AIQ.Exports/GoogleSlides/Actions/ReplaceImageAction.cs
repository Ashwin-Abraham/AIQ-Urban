using AIQ.Exports.GoogleSlides.Filters;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
public class ReplaceImageAction(ImageFilter filter, string imageSource) : IGoogleSlidesAction
{
    public ImageFilter Filter { get; } = filter;
    public string ImageSource { get; } = imageSource;

    public Request ToRequest(string presentationId)
    {
        var imageUrl = GetImageUrl();
        
        var replaceRequest = new ReplaceAllShapesWithImageRequest
        {
            ContainsText = new SubstringMatchCriteria
            {
                Text = Filter.ImageElementName,
                MatchCase = false
            },
            ImageUrl = imageUrl
        };

        return new Request { ReplaceAllShapesWithImage = replaceRequest };
    }

    private string GetImageUrl()
    {
        if (ImageSource.StartsWith("http://") || ImageSource.StartsWith("https://"))
        {
            return ImageSource;
        }

        var fileInfo = new FileInfo(ImageSource);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Image file not found: {ImageSource}");
        }

        var imageBytes = File.ReadAllBytes(ImageSource);
        var base64 = Convert.ToBase64String(imageBytes);
        var mimeType = fileInfo.Extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "image/png"
        };
        
        return $"data:{mimeType};base64,{base64}";
    }
}