using AIQ.Urban.Core.Enums;

namespace AIQ.Urban.Core.Extensions;

public static class StringExtensions
{
    public static TransitType ParseToTransitType(this string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Enum.TryParse(typeof(TransitType), name, ignoreCase: true, out var result))
            return (TransitType)result!;

        var valid = string.Join(", ", Enum.GetNames<TransitType>());
        throw new ArgumentException($"Unknown transit type '{name}'. Valid types: {valid}.");
    }
}
