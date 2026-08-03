using System.Drawing;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class UseModificationGatherStep(UseEditType editType, string name, Color color, Guid id) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new UseModificationData(editType, name, color, id)];
}
