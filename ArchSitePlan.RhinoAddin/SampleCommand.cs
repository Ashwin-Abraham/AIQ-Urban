using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.DocObjects;
using System;
using ArchSitePlan.RhinoInterface.HTML_UI.Websockets;

namespace ArchSitePlan.RhinoAddin;

public class SampleCommand:Command
{
    public SampleCommand()
    {
        Instance = this;
        WebSocketComms.StartServer();
    }

    public static SampleCommand Instance { get; private set; }

    public override string EnglishName => "SampleCommand";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return Result.Success;
    }
}

