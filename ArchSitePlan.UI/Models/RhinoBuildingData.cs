using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.oM.Geometry;
using Rhino.DocObjects;
using Rhino.Geometry;
using Mesh = Rhino.Geometry.Mesh;

namespace ArchSitePlan.UI.Models;
public class RhinoBuildingData
{
    readonly Mesh _envelopeMesh;
    readonly double _floorToFloorHeight;
    public readonly Guid ID;

    private RhinoBuildingData(
        Mesh envelopeMesh,
        double floorToFloorHeight,
        Guid meshID)
    {
        _envelopeMesh = envelopeMesh;
        _floorToFloorHeight = floorToFloorHeight;
        ID = meshID;
    }
    
    public static RhinoBuildingData? Create(RhinoObject obj, double floorToFloorHt)
    {
        if (obj.Geometry is not Mesh rhinoMesh) return null;
        return new RhinoBuildingData(rhinoMesh, floorToFloorHt, obj.Id);
    }

    public static IEnumerable<RhinoBuildingData> FromExisting(IEnumerable<RhinoObject> objs)
    {
        return objs.Select(obj => FromExisting(obj))
            .Where(x => x != null)
            .Cast<RhinoBuildingData>();
    }

    public static RhinoBuildingData? FromExisting(RhinoObject obj)
    {
        if (obj.Geometry is not Mesh rhinoMesh) return null;

        var building = CoreModel.Buildings.Find(b => b.EnvelopeId == obj.Id);

        if (building is null) return null;

        return new RhinoBuildingData(rhinoMesh, building.FloorToFloorHeight, obj.Id);
    }

    public BuildingData? ToCoreModelData()
    {
        var floorPlates = GeometryHelpers
            .GetContours(_envelopeMesh, _floorToFloorHeight)
            .Select(c => c.ToBHoM())
            .Select(c => new PlanarSurface(c, new List<ICurve>()))
            .ToList();

        return new BuildingData(
            _envelopeMesh.ToBHoM(),
            floorPlates,
            ID,
            Guid.Empty,
            _floorToFloorHeight);
    }

    public static RhinoBuildingData? FromRhinoObject(RhinoObject obj,
        int floorCount,
        double floorHeight)
    {
        if (RHGeneralConstants.BuildingEnvelopeObjectTypes.Contains(obj.ObjectType))
            return FromEnvelopeRhinoObject(obj, floorHeight);
        else
            return FromFootprintRhinoObject(obj, floorCount, floorHeight);
    }

    private static RhinoBuildingData? FromEnvelopeRhinoObject(
        RhinoObject obj, 
        double floorHeight)
    {
        Mesh rhinoMesh;

        if (obj.Geometry is Mesh x)
            rhinoMesh = x;
        else if (obj.Geometry is Brep rhinoBrep)
            rhinoMesh = GeometryHelpers.ConvertToMesh(rhinoBrep);
        else if (obj.Geometry is Rhino.Geometry.Extrusion rhinoExtrusion)
            rhinoMesh = GeometryHelpers.ConvertToMesh(rhinoExtrusion);
        else
            return null;

        var floorPlates = GeometryHelpers
                    .GetContours(rhinoMesh, floorHeight)
                    .Select(c => c.ToBHoM())
                    .Select(c => new PlanarSurface(c, new List<ICurve>()))
                    .ToList();

        return new RhinoBuildingData(rhinoMesh, floorHeight, obj.Id);
    }

    private static RhinoBuildingData? FromFootprintRhinoObject(
        RhinoObject obj, 
        int floorCount, 
        double floorHeight)
    {
        if (obj.Geometry is not Curve rhinoFootprint)
            return null;

        var extrusion = Rhino.Geometry.Extrusion.Create(
            obj.Geometry as Curve,
            Rhino.Geometry.Plane.WorldXY,
            floorCount * floorHeight,
            true);

        var rhinoMesh = GeometryHelpers.ConvertToMesh(extrusion);

        return new RhinoBuildingData(rhinoMesh, floorHeight, Guid.Empty);
    }

    internal static IEnumerable<object> BuildEmptyObjects(List<RhinoObject> removedObjects)
    {
        throw new NotImplementedException();
    }
}
