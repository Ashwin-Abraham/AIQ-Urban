using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides;
public interface IGoogleSlidesAction
{
    Request ToRequest(string presentationId);
}