namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public class ToolUseData(  string toolName, int useCount)
{
    public string ToolName { get; set; } = toolName;
    public int UseCount { get; set; } = useCount;
}
