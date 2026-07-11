using System.Drawing;

namespace AIQ.Urban.Core.Models;

public static class DefaultUses
{
    public static readonly (string Name, Color Color) Undefined = ("Undefined", Color.DarkGray);
    public static readonly (string Name, Color Color) Residential = ("Residential", Color.Gold);
    public static readonly (string Name, Color Color) Commercial = ("Commercial", Color.CornflowerBlue);
    public static readonly (string Name, Color Color) Retail = ("Retail", Color.IndianRed);
    public static readonly (string Name, Color Color) Industrial = ("Industrial", Color.SandyBrown);
    public static readonly (string Name, Color Color) MixedUse = ("MixedUse", Color.MediumPurple);
    public static readonly (string Name, Color Color) Public = ("Public", Color.MediumSeaGreen);
    public static readonly (string Name, Color Color) Park = ("Park", Color.ForestGreen);
    public static readonly (string Name, Color Color) Parking = ("Parking", Color.LightSteelBlue);
    public static readonly (string Name, Color Color) Utility = ("Utility", Color.Khaki);

    public static readonly IReadOnlyList<(string Name, Color Color)> All =
    [
        Undefined, Residential, Commercial, Retail, Industrial, MixedUse,
        Public, Park, Parking, Utility
    ];

    public static string[] Names { get; } = All.Select(x => x.Name).ToArray();

    public static List<Use> CreateDefaults()
    {
        return All.Select(x => new Use(Guid.NewGuid(), x.Name, x.Color)).ToList();
    }

    public static Use Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var match = All.FirstOrDefault(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        if (match.Name is null)
            throw new ArgumentException($"Unknown use name '{name}'.");

        return new Use(Guid.NewGuid(), match.Name, match.Color);
    }
}
