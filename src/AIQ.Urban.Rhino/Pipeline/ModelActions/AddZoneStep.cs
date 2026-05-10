using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;

public class AddZoneStep(List<ZoneData> _zoneData) : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var zones = _zoneData.Select(x => x.ToZone()).ToList();
        foreach (var zone in zones)
            model.AddZone(zone);

        return zones.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}

public record ZoneData(Polygon2D Boundary, string Name)
{
    public Zone ToZone()
    {
        return new Zone(Boundary, Name);
    }
}
