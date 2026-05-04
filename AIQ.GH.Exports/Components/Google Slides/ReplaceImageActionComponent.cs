using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class ReplaceImageActionComponent : GH_Component
{
    public ReplaceImageActionComponent()
        : base("Replace Image Action", "ReplaceImage",
               "Creates an action to replace an image in Google Slides",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.ReplaceImageAction;

    protected override Bitmap Icon => IconLoader.LoadIcon("Replace Image.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Image Filter", "F", "Image filter", GH_ParamAccess.item);
        pManager.AddTextParameter("Image Path", "I", "Image path (URL or local file path)", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Action", "A", "Replace image action", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        ImageFilterGoo? filterGoo = null;
        string? imagePath = null;

        if (!DA.GetData(0, ref filterGoo)) return;
        if (!DA.GetData(1, ref imagePath)) return;

        if (filterGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image filter is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image path cannot be empty");
            return;
        }

        var action = new ReplaceImageAction(filterGoo.Value, imagePath);
        DA.SetData(0, new ReplaceImageActionGoo(action));
    }
}