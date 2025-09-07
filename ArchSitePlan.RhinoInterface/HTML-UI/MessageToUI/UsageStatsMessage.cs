using ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public class UsageStatsMessage : IMessageToUI
{
    public string Type => nameof(UsageStatsMessage);
    public List<ToolUseData> ToolUseStats { get; set; } = [];

    public void Increment(TypeOfMessage messageType)
    {
        var toolStat = ToolUseStats.Find(x => x.ToolName == messageType.ToString());

        if(toolStat == null)
        {
            toolStat = new ToolUseData(
                toolName: messageType.ToString(),
                useCount: 1);

            ToolUseStats.Add(toolStat);
        }
        else
        {
            toolStat.UseCount++;
        }
    }
}
