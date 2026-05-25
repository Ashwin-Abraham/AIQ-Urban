using System.Diagnostics;
using AIQ.Urban.Core;
using AIQ.Urban.RhinoInterface;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Rhino;

namespace AIQ.MCP.Services;

/// <summary>Rhino-backed pipeline executor. Requires an active RhinoDoc.
/// Used in production inside the Rhino plugin; not suitable for unit tests.</summary>
public class RhinoPipelineExecutor : IPipelineExecutor
{
    public void Execute(Action<PipelineConfigurations> configure)
    {
        var doc = RhinoDoc.ActiveDoc
            ?? throw new McpProtocolException(
                "Active Rhino document is required. Ensure the MCP server is running within Rhino.",
                McpErrorCode.InternalError);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        var configs = new PipelineConfigurations(pipeline);
        configure(configs);
        configs.WithMetricsCalculation(
            saveToFile: true,
            tolerance: doc.ModelAbsoluteTolerance,
            docName: doc.Name);
        pipeline.Run();
        doc.Views.Redraw();
    }
}
