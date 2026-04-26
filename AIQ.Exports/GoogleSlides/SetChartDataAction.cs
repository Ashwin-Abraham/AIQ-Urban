using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides;
public class SetChartDataAction(TextFilter filter, List<string> seriesData) : IGoogleSlidesAction
{
    public TextFilter Filter { get; } = filter;
    public List<string> SeriesData { get; } = seriesData;

    public Request ToRequest(string presentationId)
    {
        return new Request();
    }
}