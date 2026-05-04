using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;

public class CopySlideAction(string slideId) : IGoogleSlidesAction
{
    public string SlideId { get; } = slideId;

    public Request ToRequest(Presentation presentation)
    {
        return new Request
        {
            DuplicateObject = new DuplicateObjectRequest
            {
                ObjectId = SlideId
            }
        };
    }
}
