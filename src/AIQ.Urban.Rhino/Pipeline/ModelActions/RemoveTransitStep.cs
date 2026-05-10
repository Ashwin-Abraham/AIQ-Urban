using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;

namespace AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;
public class RemoveTransitStep(List<Guid> _centerlineIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var transitsToRemove = model.Transits
            .Where(x => x.GetGeometryIds()
                .Any(id => _centerlineIds.Contains(id)))
            .ToList();

        model.Transits.RemoveAll(x => transitsToRemove.Contains(x));

        return transitsToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
