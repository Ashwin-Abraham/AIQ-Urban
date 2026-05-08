namespace AIQ.Primitives.Extensions;
public static class StringExtensions
{
    public static bool ContainsAny(
        this string source,
        List<string> toCheck, StringComparison comp = StringComparison.OrdinalIgnoreCase)
    {
        foreach (var check in toCheck)
        {
            if (source.Contains(check, comp))
                return true;
        }

        return false;
    }
}
