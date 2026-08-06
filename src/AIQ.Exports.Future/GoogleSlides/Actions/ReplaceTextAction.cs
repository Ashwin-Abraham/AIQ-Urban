using AIQ.Exports.GoogleSlides.Utils;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
public class ReplaceTextAction(
    int? slideNumber,
    TextMatchOption matchOption,
    string findText,
    string replacementText) : IGoogleSlidesAction
{
    public int? SlideNumber { get; } = slideNumber;
    public TextMatchOption MatchOption { get; } = matchOption;
    public string FindText { get; } = findText;
    public string ReplacementText { get; } = replacementText;

    public IEnumerable<Request> ToRequest(Presentation presentation)
    {
        var slideNumberToId = presentation.Slides?
            .Select((slide, i) => new { slide.ObjectId, Index = i + 1 })
            .ToDictionary(x => x.Index, x => x.ObjectId) ?? [];

        string[]? pageObjectIds = SlideNumber.HasValue &&
                                  slideNumberToId.TryGetValue(SlideNumber.Value, out var id)
                                      ? [id]
                                      : null;

        var replaceRequest = new ReplaceAllTextRequest
        {
            ContainsText = new SubstringMatchCriteria
            {
                Text = FindText,
                MatchCase = MatchOption == TextMatchOption.MatchCase
            },
            ReplaceText = ReplacementText,
            PageObjectIds = pageObjectIds
        };

        yield return new Request { ReplaceAllText = replaceRequest };
    }
}