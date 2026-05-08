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
        pManager.AddIntegerParameter("Slide Number", "S", "Slide number (1-based, 0 or empty for all slides)", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Image Element Name", "I", "Name of the image element to find", GH_ParamAccess.item);
        pManager.AddTextParameter("Image Path", "P", "Image path (URL or local file path)", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Action", "A", "Replace image action", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int slideNumber = 0;
        string? imageElementName = null;
        string? imagePath = null;

        DA.GetData(0, ref slideNumber);
        if (!DA.GetData(1, ref imageElementName)) return;
        if (!DA.GetData(2, ref imagePath)) return;

        if (string.IsNullOrWhiteSpace(imageElementName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image element name cannot be empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image path cannot be empty");
            return;
        }

        var action = new ReplaceImageAction(
            slideNumber <= 0 ? null : slideNumber,
            imageElementName,
            imagePath);
        DA.SetData(0, new ReplaceImageActionGoo(action));
    }
}