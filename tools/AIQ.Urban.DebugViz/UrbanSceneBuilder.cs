using System.Drawing;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz;

public class UrbanSceneBuilder
{
    private readonly RhinoDoc _doc;

    public UrbanSceneBuilder()
    {
        _doc = RhinoDoc.CreateHeadless(null);
    }

    public UrbanSceneBuilder AddUrbanElements(IEnumerable<IUrbanElement> elements)
    {
        foreach (var element in elements)
        {
            switch (element)
            {
                case Building building:
                    AddBuilding(building);
                    break;
                case Transit transit:
                    AddTransit(transit);
                    break;
                case Parcel parcel:
                    AddParcel(parcel);
                    break;
                case Zone zone:
                    AddZone(zone);
                    break;
            }
        }
        return this;
    }

    public UrbanSceneBuilder AddTransitSurfaces(IEnumerable<(Profile2D Profile, TransitType Type)> surfaces)
    {
        foreach (var (profile, type) in surfaces)
        {
            var color = ColorHelpers.ColorForTransitType(type);
            var layer = RhinoHelper.EnsureLayer(_doc, $"TransitSurfaces::{type}", color);
            var attribs = new ObjectAttributes { LayerIndex = layer };

            var meshes = Mesh.CreateFromBrep(profile.Geometry, MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    _doc.Objects.AddMesh(m, attribs);
        }
        return this;
    }

    public RhinoDoc Build() => _doc;

    private void AddBuilding(Building building)
    {
        var envelopeLayer = RhinoHelper.EnsureLayer(_doc, "Envelopes", Color.FromArgb(89, Color.Gray));
        var envelopeAttribs = new ObjectAttributes { LayerIndex = envelopeLayer };

        var meshes = Mesh.CreateFromBrep(building.Envelope.Geometry, MeshingParameters.FastRenderMesh);
        if (meshes != null)
            foreach (var m in meshes)
                _doc.Objects.AddMesh(m, envelopeAttribs);

        foreach (var plate in building.FloorPlates)
        {
            var color = ColorHelpers.ColorFromString(plate.Use.Name);
            var layer = RhinoHelper.EnsureLayer(_doc, $"FloorPlates::{plate.Use.Name}", color);
            var attribs = new ObjectAttributes { LayerIndex = layer };

            var breps = Brep.CreatePlanarBreps(plate.Outline.Geometry, 0.001);
            if (breps is { Length: > 0 })
            {
                var plateMeshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
                if (plateMeshes != null)
                    foreach (var m in plateMeshes)
                        _doc.Objects.AddMesh(m, attribs);
            }
        }
    }

    private void AddTransit(Transit transit)
    {
        var color = ColorHelpers.ColorForTransitType(transit.TransitType);
        var clLayer = RhinoHelper.EnsureLayer(_doc, $"TransitCenterlines::{transit.TransitType}", color);
        var clAttribs = new ObjectAttributes { LayerIndex = clLayer };
        _doc.Objects.AddCurve(transit.Centerline.Geometry, clAttribs);

        var outlineLayer = RhinoHelper.EnsureLayer(_doc, "TransitOutlines", Color.Black);
        var outlineAttribs = new ObjectAttributes { LayerIndex = outlineLayer };
        _doc.Objects.AddCurve(transit.Outline.Outline.Geometry, outlineAttribs);
    }

    private void AddParcel(Parcel parcel)
    {
        var color = ColorHelpers.ColorFromString(parcel.Use.Name);
        var layer = RhinoHelper.EnsureLayer(_doc, $"Parcels::{parcel.Use.Name}", color);
        var attribs = new ObjectAttributes { LayerIndex = layer };

        var breps = Brep.CreatePlanarBreps(parcel.Outline.Geometry, 0.001);
        if (breps is { Length: > 0 })
        {
            var meshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    _doc.Objects.AddMesh(m, attribs);
        }
    }

    private void AddZone(Zone zone)
    {
        var layer = RhinoHelper.EnsureLayer(_doc, "Zones", Color.CornflowerBlue);
        var attribs = new ObjectAttributes { LayerIndex = layer };
        _doc.Objects.AddCurve(zone.Boundary.Geometry, attribs);
    }
}
