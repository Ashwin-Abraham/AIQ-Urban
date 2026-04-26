namespace AIQ.Exports.GoogleSlides;
public class ImageFilter(
    string? slideName,
    string imageElementName)
{
    public string? SlideName { get; } = slideName;
    public string ImageElementName { get; } = imageElementName;
}