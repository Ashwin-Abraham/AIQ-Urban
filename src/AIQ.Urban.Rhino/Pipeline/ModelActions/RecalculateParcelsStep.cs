using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIQ.Urban.Core;

namespace AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;
internal class RecalculateParcelsStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var transitOutlines = model.Transits.Select(x => x.Outline);

        return [];
    }
}
