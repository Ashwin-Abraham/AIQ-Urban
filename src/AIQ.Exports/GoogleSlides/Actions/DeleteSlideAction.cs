using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;

public class DeleteSlideAction : IGoogleSlidesAction
{
    private readonly int? _slideIndex;
    private readonly string? _slideId;

    public DeleteSlideAction(string slideId)
    {
        _slideId = slideId;
    }

    public DeleteSlideAction(int slideIndex)
    {
        _slideIndex = slideIndex;
    }

    public IEnumerable<Request> ToRequest(Presentation presentation)
    {
        var slide = _slideIndex != null?
            presentation.Slides.ElementAtOrDefault(_slideIndex!.Value)
            : presentation.Slides.FirstOrDefault(x => x.ObjectId == _slideId);

        if (slide == null)
            return [];

        return 
            [
                new Request
                {
                    DeleteObject = new DeleteObjectRequest
                    {
                        ObjectId = slide.ObjectId
                    }
                }
            ];
    }
}
