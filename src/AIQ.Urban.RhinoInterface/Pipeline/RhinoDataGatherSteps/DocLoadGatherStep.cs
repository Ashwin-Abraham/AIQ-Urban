using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class DocLoadGatherStep(UrbanModel model, double _tolerance) : IRhinoDataGatherStep
{
    public List<ICreationData> Run(RhinoDoc doc)
    {
        var result = new List<ICreationData>();

        GatherTransits(doc, result);
        GatherParcelCentroids(doc, result);
        GatherBuildings(doc, result);
        GatherZones(doc, result);

        return result;
    }

    private void GatherTransits(RhinoDoc doc, List<ICreationData> result)
    {
        var transitPrefix = LayerConstants.Transit + "::";
        var centerlines = RhinoDocScanner.FindCurvesOnLayerPrefix(doc, transitPrefix)
            .Where(c => c.LayerPath.Contains("::Centerline::"))
            .ToList();

        foreach (var (curve, objectId, layerPath) in centerlines)
        {
            var parsed = LayerNameParser.ParseTransitLayer(layerPath);
            if (parsed is null) continue;

            var curve2d = new Curve2D(curve, objectId);
            result.Add(new TransitData(curve2d, parsed.Value.Width, parsed.Value.Type));
        }
    }

    private void GatherParcelCentroids(RhinoDoc doc, List<ICreationData> result)
    {
        var parcelPrefix = LayerConstants.Parcel + "::";
        var parcelBreps = RhinoDocScanner.FindBrepsOnLayerPrefix(doc, parcelPrefix);

        foreach (var (brep, objectId, layerPath) in parcelBreps)
        {
            var useName = LayerNameParser.ParseParcelUse(layerPath);
            if (useName is null) continue;
            var use = ResolveUse(useName);

            var centroid = AreaMassProperties.Compute(brep)?.Centroid;
            if (centroid.HasValue)
                result.Add(new CentroidUseData(centroid.Value, use));
        }
    }

    private void GatherBuildings(RhinoDoc doc, List<ICreationData> result)
    {
        var envelopeBreps = RhinoDocScanner.FindBrepsOnLayerPrefix(doc, LayerConstants.BuildingEnvelope);
        var floorPlateCurves = RhinoDocScanner.FindCurvesOnLayerPrefix(doc, LayerConstants.BuildingFloorPlates + "::");

        foreach (var (envelopeBrep, envelopeId, _) in envelopeBreps)
        {
            var associatedPlates = new List<FloorPlate>();
            foreach (var (fpCurve, fpId, fpLayerPath) in floorPlateCurves)
            {
                var useName = LayerNameParser.ParseFloorPlateUse(fpLayerPath);
                if (useName is null) continue;
                var use = ResolveUse(useName);

                var fpCentroid = AreaMassProperties.Compute(fpCurve)?.Centroid;
                if (fpCentroid is null) continue;

                var isInside = envelopeBrep.IsPointInside(fpCentroid.Value, doc.ModelAbsoluteTolerance, true);
                if (isInside)
                {
                    var polygon = new Polygon2D(fpCurve);
                    associatedPlates.Add(new FloorPlate(polygon, use));
                }
            }

            if (associatedPlates.Count == 0) continue;

            var envelope = new BrepElement(envelopeBrep, envelopeId);
            result.Add(new BuildingMassAndFloorsData(envelope, associatedPlates));
        }
    }

    private void GatherZones(RhinoDoc doc, List<ICreationData> result)
    {
        var zonePrefix = LayerConstants.ZoneRoot + "::";
        var zoneCurves = RhinoDocScanner.FindCurvesOnLayerPrefix(doc, zonePrefix);

        foreach (var (curve, objectId, layerPath) in zoneCurves)
        {
            var zoneName = LayerNameParser.ParseZoneName(layerPath);
            if (zoneName is null) continue;

            var polygon = new Polygon2D(curve) { Id = objectId };
            result.Add(new ZoneData(polygon, zoneName));
        }
    }

    private Use ResolveUse(string useName)
    {
        try { return model.GetUse(useName); }
        catch { return model.GetUse("Undefined"); }
    }
}
