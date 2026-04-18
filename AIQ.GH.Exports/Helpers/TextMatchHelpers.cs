using System;
using AIQ.GH.Exports.Enums;

namespace AIQ.GH.Exports.Helpers;

public static class TextMatchHelpers
{
    public static bool Matches(string? text, string? pattern, TextMatchStrategy strategy)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;
        
        if (text is null)
            return false;

        return strategy switch
        {
            TextMatchStrategy.Contains => text.Contains(pattern, StringComparison.OrdinalIgnoreCase),
            TextMatchStrategy.DoesNotContain => !text.Contains(pattern, StringComparison.OrdinalIgnoreCase),
            TextMatchStrategy.ExactMatch => text.Equals(pattern, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}