using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace AIQ.Urban.Core.Tests.ModelActions.Tools;

public static class UrbanModelVisualizers
{
    public static void VisualizeAll(this UrbanModel model)
    {
        var doc = CreateDoc();
        AddBuildings(doc, model);
        AddTransits(doc, model);
        AddTransitSurfaces(doc, model);
        AddParcels(doc, model);
        AddZones(doc, model);
#if DEBUG
        ExportAndOpen(doc);
#endif
    }

    public static void VisualizeBuildings(this UrbanModel model)
    {
        var doc = CreateDoc();
        AddBuildings(doc, model);
#if DEBUG
        ExportAndOpen(doc);
#endif
    }

    public static void VisualizeTransits(this UrbanModel model)
    {
        var doc = CreateDoc();
        AddTransits(doc, model);
#if DEBUG
        ExportAndOpen(doc);
#endif
    }

    public static void VisualizeTransitSurfaces(this UrbanModel model)
    {
        var doc = CreateDoc();
        AddTransitSurfaces(doc, model);
#if DEBUG
        ExportAndOpen(doc);
#endif
    }

    public static void VisualizeParcels(this UrbanModel model)
    {
        var doc = CreateDoc();
        AddParcels(doc, model);
#if DEBUG
        ExportAndOpen(doc);
#endif
    }

    public static void VisualizeZones(this UrbanModel model)
    {
        var doc = CreateDoc();
        AddZones(doc, model);
#if DEBUG
        ExportAndOpen(doc);
#endif
    }

    private static RhinoDoc CreateDoc()
        => RhinoDoc.CreateHeadless(null);

#if DEBUG
    private static void ExportAndOpen(RhinoDoc doc)
    {
        if (doc.Objects.Count == 0)
            return;

        var path = Path.Combine(Path.GetTempPath(), $"AIQUrban_{Guid.NewGuid():N}.glb");
        var options = new FileGltfWriteOptions
        {
            ExportLayers = true,
            MapZToY = true,
            ExportMaterials = true,
            UseDisplayColorForUnsetMaterials = true
        };

        FileGltf.Write(path, doc, options);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
#endif

    private static void AddBuildings(RhinoDoc doc, UrbanModel model)
    {
        if (model.Buildings.Count == 0)
            return;

        var envelopeLayer = EnsureLayer(doc, "Envelopes", Color.FromArgb(89, Color.Gray));
        var envelopeAttribs = new ObjectAttributes { LayerIndex = envelopeLayer };

        foreach (var building in model.Buildings)
        {
            var meshes = Mesh.CreateFromBrep(building.Envelope.Geometry, MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    doc.Objects.AddMesh(m, envelopeAttribs);

            foreach (var plate in building.FloorPlates)
            {
                var color = ColorFromString(plate.Use.Name);
                var layer = EnsureLayer(doc, $"FloorPlates::{plate.Use.Name}", color);
                var attribs = new ObjectAttributes { LayerIndex = layer };

                var breps = Brep.CreatePlanarBreps(plate.Outline.Geometry, 0.001);
                if (breps is { Length: > 0 })
                {
                    var plateMeshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
                    if (plateMeshes != null)
                        foreach (var m in plateMeshes)
                            doc.Objects.AddMesh(m, attribs);
                }
            }
        }
    }

    private static void AddTransits(RhinoDoc doc, UrbanModel model)
    {
        if (model.Transits.Count == 0)
            return;

        foreach (var transit in model.Transits)
        {
            var color = ColorForTransitType(transit.TransitType);
            var clLayer = EnsureLayer(doc, $"TransitCenterlines::{transit.TransitType}", color);
            var clAttribs = new ObjectAttributes { LayerIndex = clLayer };
            doc.Objects.AddCurve(transit.Centerline.Geometry, clAttribs);

            var outlineLayer = EnsureLayer(doc, "TransitOutlines", Color.Black);
            var outlineAttribs = new ObjectAttributes { LayerIndex = outlineLayer };
            doc.Objects.AddCurve(transit.Outline.Outline.Geometry, outlineAttribs);
        }
    }

    private static void AddTransitSurfaces(RhinoDoc doc, UrbanModel model)
    {
        if (model.TransitSurfaces.Count == 0)
            return;

        foreach (var (profile, type) in model.TransitSurfaces)
        {
            var color = ColorForTransitType(type);
            var layer = EnsureLayer(doc, $"TransitSurfaces::{type}", color);
            var attribs = new ObjectAttributes { LayerIndex = layer };

            var meshes = Mesh.CreateFromBrep(profile.Geometry, MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    doc.Objects.AddMesh(m, attribs);
        }
    }

    private static void AddParcels(RhinoDoc doc, UrbanModel model)
    {
        if (model.Parcels.Count == 0)
            return;

        foreach (var parcel in model.Parcels)
        {
            var color = ColorFromString(parcel.Use.Name);
            var layer = EnsureLayer(doc, $"Parcels::{parcel.Use.Name}", color);
            var attribs = new ObjectAttributes { LayerIndex = layer };

            var breps = Brep.CreatePlanarBreps(parcel.Outline.Geometry, 0.001);
            if (breps is { Length: > 0 })
            {
                var meshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
                if (meshes != null)
                    foreach (var m in meshes)
                        doc.Objects.AddMesh(m, attribs);
            }
        }
    }

    private static void AddZones(RhinoDoc doc, UrbanModel model)
    {
        if (model.Zones.Count == 0)
            return;

        var layer = EnsureLayer(doc, "Zones", Color.CornflowerBlue);
        var attribs = new ObjectAttributes { LayerIndex = layer };

        foreach (var zone in model.Zones)
            doc.Objects.AddCurve(zone.Boundary.Geometry, attribs);
    }

    private static int EnsureLayer(RhinoDoc doc, string path, Color color)
    {
        var idx = doc.Layers.FindByFullPath(path, -1);
        if (idx >= 0)
            return idx;
        return doc.Layers.AddPath(path, color);
    }

    private static Color ColorForTransitType(TransitType type) => type switch
    {
        TransitType.Road => Color.DimGray,
        TransitType.Railway => Color.SaddleBrown,
        TransitType.CyclePath => Color.DarkGreen,
        TransitType.Pedestrian => Color.DarkOliveGreen,
        TransitType.Waterway => Color.DodgerBlue,
        _ => ColorFromString(type.ToString()),
    };

    private static Color ColorFromString(string name)
    {
        var hash = name.GetHashCode();
        var r = (byte)((hash & 0xFF) % 200 + 55);
        var g = (byte)(((hash >> 8) & 0xFF) % 200 + 55);
        var b = (byte)(((hash >> 16) & 0xFF) % 200 + 55);
        return Color.FromArgb(r, g, b);
    }
}
