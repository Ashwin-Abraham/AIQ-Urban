using System.Text.Json;
using AIQ.Urban.RhinoInterface;

namespace AIQ.MCP.Models;
public interface IPipelineRequestBuilder
{
    public JsonElement ParseRequest();
    public RhinoUrbanModelPipeline BuildPipeline();
}
