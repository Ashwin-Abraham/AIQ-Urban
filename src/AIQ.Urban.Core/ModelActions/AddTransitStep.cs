using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions;
public class AddTransitStep : IAddUrbanElementsStep
{
    private readonly List<TransitData> _transitData;

    public AddTransitStep(List<TransitData> transitData)
    {
        _transitData = transitData;
    }

    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var transits = _transitData.Select(x => x.ToTransit()).ToList();
        model.AddTransit(transits);

        return transits.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}

public record TransitData(
     Curve2D Centerline,
     double Width,
     TransitType TransitType)
{
    public Transit ToTransit()
    {
        return new Transit(Centerline, Width, TransitType);
    }
}
