using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;

public class DeleteSlideAction(string slideId) : IGoogleSlidesAction
{
    public string SlideId { get; } = slideId;

    public Request ToRequest(string presentationId)
    {
        return new Request
        {
            DeleteObject = new DeleteObjectRequest
            {
                ObjectId = SlideId
            }
        };
    }
}
