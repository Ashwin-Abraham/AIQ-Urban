using System.Drawing;

namespace AIQ.Urban.Core.Models;

public static class DefaultUses
{
    public static readonly Use Residential = new("Residential", Color.Gold);
    public static readonly Use Commercial = new("Commercial", Color.CornflowerBlue);
    public static readonly Use Retail = new("Retail", Color.IndianRed);
    public static readonly Use Industrial = new("Industrial", Color.Gray);
    public static readonly Use MixedUse = new("MixedUse", Color.MediumPurple);
    public static readonly Use Public = new("Public", Color.MediumSeaGreen);
    public static readonly Use Park = new("Park", Color.ForestGreen);
    public static readonly Use Parking = new("Parking", Color.LightSteelBlue);
    public static readonly Use Utility = new("Utility", Color.Khaki);

    public static readonly IReadOnlyList<Use> All =
    [
        Residential, Commercial, Retail, Industrial, MixedUse,
        Public, Park, Parking, Utility
    ];

    public static readonly string[] Names = All.Select(u => u.Name).ToArray();
}
