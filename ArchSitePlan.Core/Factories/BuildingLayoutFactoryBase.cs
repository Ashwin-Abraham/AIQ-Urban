using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Comparers;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.Engine.Geometry.Triangulation;
using BH.Engine.Representation;
using BH.oM.Geometry;
using BH.oM.Graphics;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Factories;

public class BuildingLayoutFactoryBase
{
    public static double GetRequiredGFA(
        BuildingTargetMode targetMode,
        double? targetGfa,
        double? targetFar,
        double siteArea)
    {
        switch(targetMode)
        {
            case BuildingTargetMode.GFA:
                return targetGfa.HasValue? targetGfa.Value :0;
            case BuildingTargetMode.FAR:
                return siteArea 
                    * (targetFar.HasValue ? targetFar.Value : 0);
            default:
                return 0;
        }
    }

    public List<BuildingCreationSpecs> GenerateMassingForTargetGFA(
        List<PolyCurve> footprints,
        double maxFloors,
        double targetGFA)
    {
        if (footprints.Count == 0) return new();

        int maxFloorCount = (int)Math.Floor(maxFloors);

        double totalFootprintArea = footprints.Sum(x => x.Area());
        double totalGFA = 0;

        List<BuildingCreationSpecs> buildingsWIP =
            footprints
            .Select(x => new BuildingCreationSpecs(x, x.IArea(), 0))
            .ToList();

        // First pack the buildings to the brim
        var smallestFootPrint = buildingsWIP.Min(x => x.FootprintArea);

        while (totalGFA < targetGFA)
        {
            foreach (var buildingWIP in buildingsWIP)
            {
                var totalGFACheck = totalGFA + buildingWIP.FootprintArea;
                if (totalGFACheck > targetGFA)
                    break;

                if(buildingWIP.FloorCount >= maxFloorCount) 
                    continue;

                buildingWIP.FloorCount++;
                totalGFA += buildingWIP.FootprintArea;
            }

            var buildingsBelowHeight = buildingsWIP
                .Where(x => x.FloorCount < maxFloorCount)
                .ToList();

            if (buildingsBelowHeight.Count == 0) break;

            smallestFootPrint = buildingsBelowHeight.Min(x => x.FootprintArea);
            if (totalGFA + smallestFootPrint > targetGFA) break;
        }

        // Next Apply variance

        buildingsWIP.RemoveAll(x => x.FloorCount == 0);
        return buildingsWIP;
    }

    public static Polyline? GeneratePrimarySiteAxis(ICurve siteBoundary)
    {
        if (siteBoundary.ILength().AlmostZero()) return null;

        var minBox = siteBoundary.GetMinimumBoundingBox();
        var boxContours = minBox.Contour(10);

        List<Line> siteCountours = new();
        foreach (var contour in boxContours)
        {
            var linesWithin = contour.LineBooleanIntersection(siteBoundary);

            var selectedContour = linesWithin.MaxBy(x => x.Length());

            if (selectedContour != null)
                siteCountours.Add(selectedContour);
        }

        var midPoints = siteCountours.Select(x => x.Mid());
        var primaryAxis = Create.Polyline(midPoints);
        var length = primaryAxis.Length();
        primaryAxis = primaryAxis.Extend(length, length);
        var intersections = siteBoundary.ICurveIntersections(primaryAxis);
        var axisParts = primaryAxis.SplitAtPoints(intersections);
        var primaryAxisPart = axisParts
            .Where(x => siteBoundary.IIsContaining([x.Mid()]))
            .MaxBy(x => x.Length());

        return primaryAxisPart;
    }

    private static bool IsValidFootprint(ICurve curve)
    {
        if (curve.IIsClosed()) return true;

        var subParts = new List<ICurve>();

        if (curve is BoundaryCurve bCurve)
            subParts = bCurve.Curves.ToList();
        else if (curve is PolyCurve pCrv)
            subParts = pCrv.Curves.ToList();
        else
            return false;

        // Curves could be one within another, but only 1 big enclosing 1 small are accepted
        if (subParts.Count() != 2
            || subParts.Any(x => !x.IIsClosed()))
            return false;

        var bigCurve = subParts.MaxBy(x => x.IArea());
        var smallCurve = subParts.Where(x => x != bigCurve).First();

        return bigCurve.IIsContaining(smallCurve, false);
    }

    internal Building? CreateBuilding(
        BuildingCreationSpecs buildingData,
        double floorHeight)
    {
        if (!IsValidFootprint(buildingData.FootPrint))
            return null;

        Mesh envelope = CreateEnvelopeMesh(
            buildingData.FootPrint, 
            buildingData.FloorCount, 
            floorHeight);

        List<PlanarSurface> floorPlates = new();

        var typicalFloorPlate = Create.PlanarSurface(buildingData.FootPrint);

        for (int i = 0; i < buildingData.FloorCount; i++)
        {
            var mover = Vector.ZAxis * i * floorHeight;
            floorPlates.Add(typicalFloorPlate.Translate(mover));
        }

        return new Building(envelope, floorPlates, floorHeight);
    }

    public static Mesh CreateEnvelopeMesh(PolyCurve footprint, int floorCount, double floorHeight)
    {
        var mover = Vector.ZAxis * (double)floorCount * floorHeight;

        if (true || footprint.Curves.Count == 1)
        {
            var simpleExtrude = Create.Extrusion(footprint, mover, true);
            var renderMesh = BH.Engine.Representation.Compute.RenderMesh(simpleExtrude);
            return ToMesh(renderMesh);
        }

        var outerCurve = footprint.Curves.MaxBy(x => x.IArea()).IToPolyline();
        var innerCurve = footprint.Curves.First(x => x != outerCurve).IToPolyline();

        var faces = outerCurve.DelaunayTriangulation([innerCurve]);
        
        var vertices = faces
            .SelectMany(x => x.Vertices())
            .Distinct(PointComparer.Instance)
            .ToList();

        var faceIds = faces
            .Select(face => FindTriangleVertexIndices(face, vertices))
            .Where(faceIds => faceIds.Count == 3);

        var baseSurface = Create.PlanarSurface(outerCurve, [innerCurve]);
        var topSurface = baseSurface.Translate(mover);
        var outerFaces = Create.Extrusion(outerCurve, mover, false);
        var innerFaces = Create.Extrusion(innerCurve, mover, false);
        
        var compositeGeometry = Create.CompositeGeometry(new List<IGeometry>()
        {
            baseSurface,
            topSurface,
            innerFaces,
            outerFaces
        });

        return ToMesh(compositeGeometry.RenderMesh());
    }

    private static List<int> FindTriangleVertexIndices(Polyline polylineTriangles, List<Point> vertices)
    {
        List<int> ids = new();
        var polylinePoints = polylineTriangles.Vertices();
        if (polylinePoints.Count != 3) return new();

        foreach(var point in polylinePoints)
        {
            var index = vertices.FindIndex(x => point.AlmostEquals(x));
            if (index == -1) return new();
            ids.Add(index);
        }

        return ids;
    }

    private static Mesh ToMesh(RenderMesh renderMesh)
    {
        return Create.Mesh(renderMesh.Vertices.Select(x => x.Point).ToList(), renderMesh.Faces); 
    }
}