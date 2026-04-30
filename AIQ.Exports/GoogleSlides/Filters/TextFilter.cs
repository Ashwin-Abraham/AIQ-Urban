using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Filters;
public enum TextMatchOption
{
    MatchCase,
    DontMatchCase
}

public class TextFilter(
    string? slideName,
    string textElementName,
    TextMatchOption matchOption)
{
    public string? SlideName { get; } = slideName;
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