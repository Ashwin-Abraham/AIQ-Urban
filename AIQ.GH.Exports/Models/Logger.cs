using System.Collections.Generic;
using Grasshopper.Kernel;

namespace AIQ.GH.Exports.Models;
public class Logger
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Information { get; } = [];

    public void AddError(string message) => Errors.Add(message);

    public void AddWarning(string message) => Warnings.Add(message);

    public void AddInformation(string message) => Information.Add(message);

    public void LogMessages(GH_Component component)
    {
        foreach(var message in Errors)
            component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);

        foreach(var message in Warnings)
            component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

        foreach(var message in Information)
            component.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, message);
    }
}
