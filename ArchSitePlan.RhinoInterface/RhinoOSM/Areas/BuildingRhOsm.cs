using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.RhinoOSM.Areas;

public class BuildingRhOsm:RhinoOsm
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly double _defaultStoreyHeight;
    readonly int _defaultLevels;

    public BuildingRhOsm(
        BuildingOsm osmInput,
        RhinoDoc doc,
        double defaultStoreyHeight = 3.5,
        int defaultLevels = 4):base(doc)
    {
        _defaultStoreyHeight = defaultStoreyHeight;
        _defaultLevels = defaultLevels;
        _geometry = BuildGeometry(osmInput);

        SetObjectAttributes(LayerConstants.BuildingOsmLayer,
            osmInput.Color,
            osmInput.Id);
    }

    private Extrusion? BuildGeometry(BuildingOsm osmInput)
    {
        double height;
        double seed = new Random().NextDouble() * 0.4 + 0.8;

        if (osmInput.Height.HasValue)
            height = osmInput.Height.Value;
        else if (osmInput.Levels.HasValue)
            height = osmInput.Levels.Value * _defaultStoreyHeight;
        else
            height = _defaultStoreyHeight * _defaultLevels * seed;

        var planarSurface = osmInput.Boundary.ToOSMRhino();

        if (planarSurface is null) return null;

        return Extrusion.Create(planarSurface, Plane.WorldXY, height, true);
    }
}
