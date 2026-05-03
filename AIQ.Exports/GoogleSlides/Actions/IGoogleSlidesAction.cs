using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
public interface IGoogleSlidesAction
{
    Request ToRequest(string presentationId, Dictionary<int, string> slideNumberToId);
}