using AIQ.GH.Exports.Models;
using Grasshopper.Kernel;
using Rhino;

namespace AIQ.GH.Exports.Extensions;
public static class LoggerExtensions
{
    public static bool ValidateRhinoDoc(this Logger logger, RhinoDoc doc, GH_Component component)
    {
        if (doc is null)
        {
            logger.AddError("No active Rhino document");
            return false;
        }

        return true;
    }
}
