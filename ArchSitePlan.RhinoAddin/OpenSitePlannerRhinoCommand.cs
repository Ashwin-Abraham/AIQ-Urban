using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace ArchSitePlan.RhinoAddin
{
    public class OpenSitePlannerRhinoCommand : Command
    {
        public OpenSitePlannerRhinoCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static OpenSitePlannerRhinoCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "AIQ_Urban";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Loaded plugin");
            Rhino.UI.Panels.OpenPanel(MainViewHost.PanelId);
            return Result.Success;
        }
    }
}
