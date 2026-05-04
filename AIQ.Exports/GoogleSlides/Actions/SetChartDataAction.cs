using AIQ.Exports.GoogleSlides.Utils;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
public class SetChartDataAction(
    int? slideNumber,
    TextMatchOption matchOption,
    List<string> seriesData) : IGoogleSlidesAction
{
    public int? SlideNumber { get; } = slideNumber;
    public TextMatchOption MatchOption { get; } = matchOption;
    public List<string> SeriesData { get; } = seriesData;

    public Request ToRequest(string presentationId, Dictionary<int, string> slideNumberToId)
    {
        return new Request();
    }
}