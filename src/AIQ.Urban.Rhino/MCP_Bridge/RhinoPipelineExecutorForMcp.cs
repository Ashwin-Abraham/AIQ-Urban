using System;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface;
using Rhino;

namespace AIQ.Urban.RhinoInterface.MCP_Bridge;

public class RhinoPipelineExecutorForMcp(RhinoDoc doc, UrbanModel model)
{
    public ProjectMetrics Run(Action<RhinoUrbanModelPipeline> pipelineAction)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, model);
        pipelineAction(pipeline);
        pipeline.Run();
        doc.Views.Redraw();
        return model.ProjectMetrics!;
    }
}
