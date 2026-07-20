using System;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

public class RhinoPipelineExecutorForMcp(RhinoDoc doc, UrbanModel model)
{
    public ProjectMetrics Run(Action<RhinoUrbanModelPipeline> pipelineAction)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(doc, model, loggerFactory);
        pipelineAction(pipeline);
        pipeline.Run();
        doc.Views.Redraw();
        return model.ProjectMetrics!;
    }
}
