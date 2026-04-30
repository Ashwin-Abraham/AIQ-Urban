using AIQ.Exports.GoogleSlides.Filters;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
public class ReplaceTextAction(TextFilter filter, string findText, string replacementText) : IGoogleSlidesAction
{
    public TextFilter Filter { get; } = filter;
    public string FindText { get; } = findText;
    public string ReplacementText { get; } = replacementText;

    public Request ToRequest(string presentationId)
    {
        var replaceRequest = new ReplaceAllTextRequest
        {
            ContainsText = Filter.ToSubstringMatchCriteria(FindText),
            ReplaceText = ReplacementText
        };

        return new Request { ReplaceAllText = replaceRequest };
    }
}