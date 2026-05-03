using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides;
using AIQ.GH.Exports.Types;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class GoogleSlidesDocComponent : GH_Component
{
    public GoogleSlidesDocComponent()
        : base("Google Slides Doc", "GSlideDoc",
               "Creates a Google Slides document handle",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.GoogleSlidesDoc;

    protected override Bitmap Icon => IconLoader.LoadIcon("Google Slides.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Presentation ID", "ID", "Google Slides presentation ID or URL", GH_ParamAccess.item);
        pManager.AddTextParameter("Service Account JSON", "Auth", "Google service account JSON credentials", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Document", "Doc", "Google Slides document handle", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string? presentationId = null;
        string? serviceAccountJson = null;

        if (!DA.GetData(0, ref presentationId)) return;
        if (!DA.GetData(1, ref serviceAccountJson)) return;

        if (string.IsNullOrWhiteSpace(presentationId))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Presentation ID cannot be empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Service account JSON cannot be empty");
            return;
        }

        var cleanedId = presentationId.Trim();
        if (cleanedId.StartsWith("https://docs.google.com/presentation/d/"))
        {
            var parts = cleanedId.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "d" && i + 1 < parts.Length)
                {
                    cleanedId = parts[i + 1].TrimEnd('/');
                    break;
                }
            }
        }

        var doc = new GoogleSlidesDocument(cleanedId, serviceAccountJson);

        var valid = AsyncHelper.RunSync(ct => doc.ValidateAsync(ct), Constants.CommonToken);
        if (!valid)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to access presentation. Check ID and credentials.");
            return;
        }
        
        DA.SetData(0, new GoogleSlidesDocumentGoo(doc));
    }
}