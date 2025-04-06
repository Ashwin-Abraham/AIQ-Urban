using ArchSitePlan.Core.Constants;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;

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
    public ModellingTool AppMode { get;set; }
    public UIViewModel()
    {
        string activeZoneName = CoreGeneralConstants.FullProjectStudyName;
    }
}
