using AIQ.Urban.Core;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public interface IUrbanModelRhinoOutputter
{
    void Run(UrbanModel model, RhinoDoc doc);
}
