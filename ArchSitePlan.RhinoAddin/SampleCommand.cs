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
        return Result.Success;
    }
}

