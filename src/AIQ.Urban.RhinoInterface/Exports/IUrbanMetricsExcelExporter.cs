using AIQ.Urban.Core;

namespace AIQ.Urban.RhinoInterface.Exports;

public interface IUrbanMetricsExcelExporter
{
    void Export(
        UrbanModel urbanModel,
        string filePath,
        string projectName,
        DateTime exportedAt);
}
