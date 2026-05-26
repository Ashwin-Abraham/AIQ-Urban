using AIQ.Urban.Core;
using AIQ.Urban.RhinoInterface;
using Rhino;

namespace AIQ.MCP.Services;

/// <summary>Rhino-backed pipeline executor. Requires an active RhinoDoc.
/// Used in production inside the Rhino plugin; not suitable for unit tests.</summary>
public class RhinoPipelineExecutor(RhinoDoc doc, UrbanModel model)
{
    public bool Run(Action<RhinoUrbanModelPipeline> pipelineAction)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, model);
        pipelineAction(pipeline);
        pipeline.Run();

        return true;
    }
}
