using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoBhomInterop;
using BH.oM.Geometry;
using Rhino.DocObjects;
using Rhino.Geometry;
using Mesh = Rhino.Geometry.Mesh;
using Point = BH.oM.Geometry.Point;
using BH.Engine.Geometry;
using ArchSitePlan.Core.Models.CreationData;

namespace ArchSitePlan.RhinoInterface.Models;
public static class BuildingDataCreator
{
    public static List<BuildingData> FromExistingCoreModelBuildings(IEnumerable<RhinoObject> envelopeMeshes)
    {
        var newBuildingData = new List<BuildingData>();

        foreach(var obj in envelopeMeshes)
        {
            var building = CoreModelSingelton.Instance.Buildings.Find(x => x.EnvelopeId == obj.Id);

            if (building == null)
                continue;

            var existingFloorplatesAndStart = building.FloorPlates
                .Select(x => (x, x.Profile.ExternalBoundary.IStartPoint()))
                .ToList();

            var rhinoMesh = obj.Geometry as Mesh;
            if (rhinoMesh == null)
                continue;

            var bhomMesh = rhinoMesh.ToBHoM();

            var createdFloorPlates = CreateFloorplates(rhinoMesh, building.FloorToFloorHeight);

            foreach (var floorplate in createdFloorPlates)
            {
                var closestFloorplate = FindClosesFloorplate(floorplate, existingFloorplatesAndStart);
                floorplate.Use = AsLanduseData(closestFloorplate?.Use?? AppLanduses.Undefined);
            }

            var buildingData = new BuildingData(
                bhomMesh, 
                createdFloorPlates, 
                building.EnvelopeId, 
                building.FootprintId, 
                building.FloorToFloorHeight);

            newBuildingData.Add(buildingData);
        }

        return newBuildingData;
    }

    private static LanduseData AsLanduseData(Landuse landuse)
    {
        return new LanduseData(
            name: landuse.Name,
            argbColor: landuse.Color.ToArgb(),
            id: landuse.Id);
    }

    private static List<FloorplateData> CreateFloorplates(Mesh envelope, double floorHeight)
    {
        return GeometryHelpers
            .GetContours(envelope, floorHeight)
            .Select(c => c.ToBHoM())
            .Select(c => new PlanarSurface(c, new List<ICurve>()))
            .Select(x => new FloorplateData(x, Guid.Empty, null))
            .ToList();
    }

    private static FloorPlate? FindClosesFloorplate(
        FloorplateData floorplate,
        List<(FloorPlate Floor, Point StartPoint)> bhomFloorplates)
    {
        if(bhomFloorplates.Count == 0)
            return null;

        var floorplatePoint = floorplate.Geometry.ExternalBoundary.IStartPoint();

        var closest = bhomFloorplates
            .OrderBy(x => Math.Abs(x.StartPoint.Z - floorplatePoint.Z))
            .ThenBy(x => x.StartPoint.Distance(floorplatePoint))
            .First();

        return closest.Floor;
    }

    public static BuildingData? FromRhinoObject(
        RhinoObject obj,
        int floorCount,
        double floorHeight)
    {
        if (RHGeneralConstants.BuildingEnvelopeObjectTypes.Contains(obj.ObjectType))
            return FromEnvelopeRhinoObject(obj, floorCount, floorHeight);
        else
            return FromFootprintRhinoObject(obj, floorCount, floorHeight);
    }

    private static BuildingData? FromEnvelopeRhinoObject(
        RhinoObject obj, 
        int floorCount,
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

        GeometryHelpers.RescaleMesh(rhinoMesh, (double)floorCount * floorHeight);

        var floorplates = CreateFloorplates(rhinoMesh, floorHeight);

        return new BuildingData(rhinoMesh.ToBHoM(), floorplates, obj.Id, Guid.Empty, floorHeight);
    }

    private static BuildingData? FromFootprintRhinoObject(
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

        var baseCrv = rhinoFootprint.ToBHoM();
        var floorplates = new List<FloorplateData>();

        for (int i =0; i< floorCount; i++)
        {
            var moveVector = BH.Engine.Geometry.Create.Vector(0, 0, i * floorHeight);
            var floorCrv = baseCrv.ITranslate(moveVector);
            floorplates.Add(new FloorplateData(
                new PlanarSurface(floorCrv, new List<ICurve>()),
                Guid.Empty,
                null));
        }

        return new BuildingData(rhinoMesh.ToBHoM(), floorplates, Guid.Empty, obj.Id, floorHeight);
    }
}
