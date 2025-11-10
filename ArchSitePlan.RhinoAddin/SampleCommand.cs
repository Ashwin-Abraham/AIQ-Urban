using AIQ.Exports;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.Exports;
using Rhino;
using Rhino.Commands;

namespace ArchSitePlan.RhinoAddin;

public class SampleCommand:Command
{
    public SampleCommand()
    {
        Instance = this;
    }

    public static SampleCommand Instance { get; private set; }

    public override string EnglishName => "SampleCommand";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var path = @"C:\Users\AshwinAbraham\Downloads\TestFile";
        
        // PowerpointExporter.ExportToPowerpoint(path);
        ModelBuilderPipelines.CreateNamedViewsOfEachZone(doc);
        ModelBuilderPipelines.ExportNamedViews(doc);

        return Result.Success;
    }
}

