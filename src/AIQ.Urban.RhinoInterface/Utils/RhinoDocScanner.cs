using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Utils;

internal static class RhinoDocScanner
{
    /// <summary>
    /// Finds all curves on layers whose path starts with the given prefix.
    /// </summary>
    public static List<(Curve Curve, Guid ObjectId, string LayerPath)> FindCurvesOnLayerPrefix(
        RhinoDoc doc, string layerPrefix)
    {
        var results = new List<(Curve Curve, Guid ObjectId, string LayerPath)>();
        foreach (var obj in doc.Objects)
        {
            if (!obj.ObjectType.HasFlag(ObjectType.Curve))
                continue;

            var layerIndex = obj.Attributes.LayerIndex;
            var layerPath = GetLayerPath(doc, layerIndex);
            if (layerPath == null || !layerPath.StartsWith(layerPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var curve = obj.Geometry as Curve;
            if (curve == null)
                continue;

            results.Add((curve.DuplicateCurve(), obj.Id, layerPath));
        }
        return results;
    }

    /// <summary>
    /// Finds all Breps on layers whose path starts with the given prefix.
    /// </summary>
    public static List<(Brep Brep, Guid ObjectId, string LayerPath)> FindBrepsOnLayerPrefix(
        RhinoDoc doc, string layerPrefix)
    {
        var results = new List<(Brep Brep, Guid ObjectId, string LayerPath)>();
        foreach (var obj in doc.Objects)
        {
            if (!obj.ObjectType.HasFlag(ObjectType.Brep))
                continue;

            var layerIndex = obj.Attributes.LayerIndex;
            var layerPath = GetLayerPath(doc, layerIndex);
            if (layerPath == null || !layerPath.StartsWith(layerPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var brep = obj.Geometry as Brep;
            if (brep == null)
                continue;

            results.Add((brep.DuplicateBrep(), obj.Id, layerPath));
        }
        return results;
    }

    /// <summary>
    /// Finds all objects on layers whose path starts with the given prefix.
    /// Returns the full layer path for each object.
    /// </summary>
    public static List<(GeometryBase Geometry, Guid ObjectId, string LayerPath, ObjectType ObjectType)> FindObjectsOnLayerPrefix(
        RhinoDoc doc, string layerPrefix)
    {
        var results = new List<(GeometryBase Geometry, Guid ObjectId, string LayerPath, ObjectType ObjectType)>();
        foreach (var obj in doc.Objects)
        {
            var layerIndex = obj.Attributes.LayerIndex;
            var layerPath = GetLayerPath(doc, layerIndex);
            if (layerPath == null || !layerPath.StartsWith(layerPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add((obj.Geometry.Duplicate(), obj.Id, layerPath, obj.ObjectType));
        }
        return results;
    }

    private static string? GetLayerPath(RhinoDoc doc, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= doc.Layers.Count)
            return null;
        return doc.Layers[layerIndex].FullPath;
    }
}
