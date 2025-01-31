using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Metrics;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using Rhino;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.HtmlCommsClasses;

/// <summary>
/// This represents the data required to keep the rhino model in sync with the UI's
/// displayed metrics, paths( as web tech cant access local folder structures well),
/// and other data.
/// 
/// Using a collection of IMessageToUI s only because all the data here is intended
/// to be updated in this class and then sent out as needed from here.
/// </summary>
public class UIViewModel
{
    public ModellingTool ModelToolMode { get; private set; }
    public MetricsMessage MetricsMessageData { get; private set; }
    public ZonesInModelMessage ZoneMessageData { get; private set; }
    public ExportLocationMessage ExportLocationMessageData { get; private set; }
    public string ModelFileName { get; private set; }
    public string ExportsFolder { get; private set; }

    public UIViewModel()
    {
        string activeZoneName = CoreGeneralConstants.FullProjectStudyName;
        ZoneMessageData = new ZonesInModelMessage(activeZoneName);
        MetricsMessageData = new MetricsMessage(activeZoneName);
        ExportLocationMessageData = new ExportLocationMessage();
    }

    internal void UpdateGraphMessage()
    {
        MetricsMessageData = new MetricsMessage(ZoneMessageData.ZoneName);
    }

    public void UpdateZonesInModelMessage(string activeZone)
    {
        ZoneMessageData = new ZonesInModelMessage(activeZone);
    }

    public void SetModellingMode(ModellingTool newAppTool)
    {
        if (newAppTool == ModellingTool.None)
            LayerManager.ToggleLockModelLayers(RhinoDoc.ActiveDoc, true);

        LayerManager.ToggleLayerStateBasedOnAppMode(RhinoDoc.ActiveDoc, newAppTool);
        RhinoDoc.ActiveDoc.Views.Redraw();
    }
}
