using AIQ.Urban.RhinoInterface;

namespace AIQ.MCP.Services;

/// <summary>Adapter that owns the pipeline lifecycle.
/// Behind this seam, RhinoDoc access, pipeline construction, execution,
/// and redraw are concentrated.</summary>
public interface IPipelineExecutor
{
    /// <summary>Executes pipeline steps through the full cascade.
    /// The configure callback receives a PipelineConfigurations builder for
    /// adding steps. Metrics calculation is added automatically. Returns
    /// after pipeline.Run() and doc.Views.Redraw().</summary>
    void Execute(Action<PipelineConfigurations> configure);
}
