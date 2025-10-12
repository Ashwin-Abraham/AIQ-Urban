using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;

namespace ArchSitePlan.RhinoInterface.HTML_UI.Websockets;
public static class UsageLogEmitter
{
    private static bool _isRunning = false;
    private static string _filePath = FilePathConstants.GetUsageLogPath();

    public static async void RunPeriodicUsageLogEmitter()
    {
        if(_isRunning) 
            return;

        _isRunning = true;
        BroadcastUsageMessage();

        var periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(2));

        while (await periodicTimer.WaitForNextTickAsync())
        {
            BroadcastUsageMessage();
        }
    }

    private static void BroadcastUsageMessage()
    {
        // Check cumulative document for additional usage stats from previous sessions
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath)!;
            var previousStats = JsonConvert.DeserializeObject<UsageStatsMessage>(json);
            RhinoEventHandler.UseStatsMessage.Combine(previousStats);
        }

        // Emit usage stats message to UI via websockets
        // Failure would be logged by the message received from the websocket in
        // UsageStatsBroadcastResultMessage
        WebSocketComms.BroadcastMessage(RhinoEventHandler.UseStatsMessage);

        // Reset stats message
        RhinoEventHandler.UseStatsMessage = new UsageStatsMessage();
    }
}
