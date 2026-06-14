using System.Drawing;
using AIQ.Urban.Core.Enums;

namespace AIQ.Urban.DebugViz;

internal static class PreviewConstants
{
    internal const string AiqUrbanRoot = "AIQ Urban";

    // --- Building layers ---
    internal const string BuildingLayer = "AIQ Urban::Building";
    internal const string BuildingEnvelopeLayer = "AIQ Urban::Building::Envelope";
    internal const string BuildingFloorPlatesLayer = "AIQ Urban::Building::FloorPlates";

    // --- Transit layers ---
    internal const string TransitLayer = "AIQ Urban::Transit";
    internal static string TransitCenterlineLayer(TransitType type) => $"{TransitLayer}::{type}::Centerline";
    internal static string TransitOutlineLayer(TransitType type) => $"{TransitLayer}::{type}::Outline";
    internal static string TransitSurfaceLayer(TransitType type) => $"{TransitLayer}::{type}::Surface";

    // --- Parcel layers ---
    internal const string ParcelLayer = "AIQ Urban::Parcel";

    // --- Zone layers ---
    internal const string ZoneLayer = "AIQ Urban::Zone";

    // --- Debug layers (geometry-level) ---
    internal const string DebugDefaultLayer = "Default";
    internal const string DebugCurveLayer = "Default:Curves";
    internal const string DebugBrepLayer = "Default:Breps";
    internal const string DebugProfileLayer = "Default:Profiles";

    // --- Default colors ---
    internal static readonly Color DebugGeometryColor = Color.FromArgb(15, Color.Gray);
    internal static readonly Color ZoneDefaultColor = Color.CornflowerBlue;
}
