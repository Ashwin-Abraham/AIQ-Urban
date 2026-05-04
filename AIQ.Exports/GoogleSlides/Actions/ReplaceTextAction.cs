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

    public Request ToRequest(string presentationId, Dictionary<int, string> slideNumberToId)
    {
        var replaceRequest = new ReplaceAllTextRequest
        {
            ContainsText = new SubstringMatchCriteria
            {
                Text = FindText,
                MatchCase = MatchOption == TextMatchOption.MatchCase
            },
            ReplaceText = ReplacementText,
            PageObjectIds = SlideNumber.HasValue && 
                           slideNumberToId.TryGetValue(SlideNumber.Value, out var id) 
                               ? [id] 
                               : null
        };

        return new Request { ReplaceAllText = replaceRequest };
    }
}