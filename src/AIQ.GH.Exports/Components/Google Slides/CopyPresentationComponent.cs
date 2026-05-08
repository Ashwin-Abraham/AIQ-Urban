using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class CopyPresentationComponent : GH_Component
{
    public CopyPresentationComponent()
        : base("Copy Presentation", "CopyDoc",
               "Creates a copy of a Google Slides presentation",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CopyPresentation;

    protected override Bitmap Icon => IconLoader.LoadIcon("Copy Presentation.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Document", "Doc", "Google Slides document to copy", GH_ParamAccess.item);
        pManager.AddTextParameter("Name", "N", "Optional name for the copied presentation", GH_ParamAccess.item);
        pManager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Document", "Doc", "Copied Google Slides document", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GoogleSlidesDocumentGoo? docGoo = null;
        string? name = null;

        if (!DA.GetData(0, ref docGoo)) return;
        DA.GetData(1, ref name);

        if (docGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Document is null");
            return;
        }

        try
        {
            var newDoc = AsyncHelper.RunSync(ct => docGoo.Value.CopyPresentationAsync(name, ct), Constants.CommonToken);
            DA.SetData(0, new GoogleSlidesDocumentGoo(newDoc));
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to copy presentation: {ex.Message}");
        }
    }
}
