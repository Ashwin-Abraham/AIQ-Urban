using System.Drawing;
using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Enums;

namespace AIQ.Urban.Core.Utils;

public static class ColorUtils
{
    public static Color BuildingEnvelopeColor { get; } = Color.LightGray.WithAlpha(0.75);

    public static Color DefaultZoneColor { get; } = Color.LightGray;

    public static Color GetTransitTypeColor(TransitType transitType)
    {
        return transitType switch
        {
            TransitType.Road => Color.DarkGray,
            TransitType.Railway => Color.SaddleBrown,
            TransitType.CyclePath => Color.ForestGreen,
            TransitType.Pedestrian => Color.Goldenrod,
            TransitType.Waterway => Color.CornflowerBlue,
            _ => Color.Black
        };
    }
}
