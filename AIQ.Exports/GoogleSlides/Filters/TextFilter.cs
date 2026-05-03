using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Filters;
public enum TextMatchOption
{
    MatchCase,
    DontMatchCase
}

public class TextFilter(
    int? slideNumber,
    string textElementName,
    TextMatchOption matchOption)
{
    public int? SlideNumber { get; } = slideNumber;
    public string TextElementName { get; } = textElementName;
    public TextMatchOption MatchOption { get; } = matchOption;

    public SubstringMatchCriteria ToSubstringMatchCriteria(string findText)
    {
        return new SubstringMatchCriteria
        {
            Text = findText,
            MatchCase = MatchOption == TextMatchOption.MatchCase
        };
    }
}