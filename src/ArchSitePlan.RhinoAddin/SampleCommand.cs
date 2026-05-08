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
        var path = @"C:\Users\AshwinAbraham\Downloads\";
        ModelBuilderPipelines.CreateNamedViewsOfEachZone(doc);
        ModelBuilderPipelines.ExportNamedView(doc, "AIQ_NamedView_A1", path);

        ModelBuilderPipelines.ExportPresentationPipeline(
            doc,
            saveFolder: @"C:\Users\AshwinAbraham\Downloads\",
            logoPath: null,
            authorName: "Ashwin From Rhino",
            projectName: "AIQ Export City");

        return Result.Success;
    }
}

