using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface;
using Rhino;

namespace AIQ.MCP.Services;

/// <summary>Rhino-backed pipeline executor. Requires an active RhinoDoc.
/// Used in production inside the Rhino plugin; not suitable for unit tests.</summary>
public class RhinoPipelineExecutor(RhinoDoc doc, UrbanModel model)
{
    public ProjectMetrics Run(Action<RhinoUrbanModelPipeline> pipelineAction)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, model);
        pipelineAction(pipeline);
        pipeline.Run();

        return model.ProjectMetrics!;
    }
}
