using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides.Filters;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class ImageFilterComponent : GH_Component
{
    public ImageFilterComponent()
        : base("Image Filter", "ImageFilter",
               "Creates a filter for image elements in Google Slides",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.ImageFilter;

    protected override Bitmap Icon => IconLoader.LoadIcon("Image Filter.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Slide Number", "S", "Slide number (1-based, 0 or empty for all slides)", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Image Element Name", "I", "Name of the image element to find", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Filter", "F", "Image filter", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int slideNumber = 0;
        string? imageElementName = null;

        DA.GetData(0, ref slideNumber);
        if (!DA.GetData(1, ref imageElementName)) return;

        if (string.IsNullOrWhiteSpace(imageElementName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Image element name cannot be empty");
            return;
        }

        var filter = new ImageFilter(
            slideNumber <= 0 ? null : slideNumber,
            imageElementName);

        DA.SetData(0, new ImageFilterGoo(filter));
    }
}