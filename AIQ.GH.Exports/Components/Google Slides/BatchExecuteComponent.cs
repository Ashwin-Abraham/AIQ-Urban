using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class BatchExecuteComponent : GH_Component
{
    public BatchExecuteComponent()
        : base("Batch Execute", "BatchExec",
               "Executes all pending actions on the Google Slides document",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.BatchExecute;

    protected override Bitmap Icon => IconLoader.LoadIcon("Batch Execute.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Document", "Doc", "Google Slides document with pending actions", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Execute", "X", "True to execute actions, False to skip", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "S", "True if execution succeeded", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Modified Count", "M", "Number of items modified", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GoogleSlidesDocumentGoo? docGoo = null;
        bool execute = false;

        if (!DA.GetData(0, ref docGoo)) return;
        DA.GetData(1, ref execute);

        if (docGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Document is null");
            DA.SetData(0, false);
            DA.SetData(1, 0);
            return;
        }

        if (!execute)
        {
            DA.SetData(0, true);
            DA.SetData(1, 0);
            return;
        }

        var doc = docGoo.Value;
        
        try
        {
            var count = AsyncHelper.RunSync(ct => doc.ExecuteAsync(ct), Constants.CommonToken);
            DA.SetData(0, true);
            DA.SetData(1, count);
            DA.SetData(0, new GoogleSlidesDocumentGoo(doc));
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Execution failed: {ex.Message}");
            DA.SetData(0, false);
            DA.SetData(1, 0);
        }
    }
}