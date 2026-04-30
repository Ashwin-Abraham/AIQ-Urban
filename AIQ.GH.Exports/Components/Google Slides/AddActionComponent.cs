using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Types;
using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class AddActionComponent : GH_Component
{
    public AddActionComponent()
        : base("Add Action", "AddAction",
               "Adds an action to the document's pending action queue",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.AddAction;

    protected override Bitmap Icon => IconLoader.LoadIcon("Add Action.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Document", "Doc", "Google Slides document", GH_ParamAccess.item);
        pManager.AddGenericParameter("Action", "A", "Action to add to queue", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Document", "Doc", "Updated document with action added", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GoogleSlidesDocumentGoo? docGoo = null;
        IGH_Goo? actionGoo = null;

        if (!DA.GetData(0, ref docGoo)) return;
        if (!DA.GetData(1, ref actionGoo)) return;

        if (docGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Document is null");
            return;
        }

        if (actionGoo is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Action is null");
            return;
        }

        IGoogleSlidesAction? action = actionGoo switch
        {
            ReplaceTextActionGoo rtg => rtg.Value,
            ReplaceImageActionGoo rig => rig.Value,
            SetChartDataActionGoo scd => scd.Value,
            _ => null
        };

        if (action is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid action type");
            return;
        }

        docGoo.Value.AddAction(action);
        DA.SetData(0, new GoogleSlidesDocumentGoo(docGoo.Value));
    }
}