using ArchSitePlan.Licensing;
using ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
public class UsageStatsMessage : IMessageToUI
{
    public string Type => nameof(UsageStatsMessage);
    public string UserId => MachineCodeHelpers.GetMacAddress();
    public string LastUsed => DateTime.UtcNow.ToString();
    public List<ToolUseData> ToolUseStats { get; set; } = [];

    public void Increment(string messageType)
    {
        var toolStat = ToolUseStats.Find(x => x.ToolName == messageType);

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

    internal void Combine(UsageStatsMessage? previousStats)
    {
        throw new NotImplementedException();
    }
}
