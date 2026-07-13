using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public interface IUrbanModelRhinoOutputter
{
    void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements);
}
