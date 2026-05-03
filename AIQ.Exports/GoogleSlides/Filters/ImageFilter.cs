namespace AIQ.Exports.GoogleSlides.Filters;
public class ImageFilter(
    int? slideNumber,
    string imageElementName)
{
    public int? SlideNumber { get; } = slideNumber;
    public string ImageElementName { get; } = imageElementName;
}