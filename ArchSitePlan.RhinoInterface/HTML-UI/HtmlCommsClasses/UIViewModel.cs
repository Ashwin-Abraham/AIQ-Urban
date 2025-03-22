using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Rhino;
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
    public ModellingTool AppMode { get; set; }
    public UIViewModel()
    {
        string activeZoneName = CoreGeneralConstants.FullProjectStudyName;
    }

    public void ReloadApp()
    {
        RhinoDoc doc = RhinoDoc.ActiveDoc;
        // AppEnabled = !AppEnabled;

        //bool expanderOpen = AppMode != ModellingTool.None;
        // AppMode = ModellingTool.None;

        //SetAppMode(AppMode, expanderOpen);

        //if (!AppEnabled || AppMode == ModellingTool.None)
        //{
            // Hard lock to avoid complex refactor
            LayerManager.ToggleLockModelLayers(doc, true);
        //    return;
        //}

        ModelBuilderPipelines.ReloadAppPipeline(doc, /*AppMode*/ ModellingTool.None);
        // ActiveStudy = CoreModel.Studies.First();
        //ActiveStudyAreaName = ActiveStudy.Name;
        //StudyAreasNames = CoreModel.Studies.Select(s => s.Name).ToList();
        LayerManager.ToggleLayerStateBasedOnAppMode(doc, /*AppMode*/ ModellingTool.None);
    }
}
