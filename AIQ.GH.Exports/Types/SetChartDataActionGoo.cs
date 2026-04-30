using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.GH.Exports.Types;
public class SetChartDataActionGoo : GH_Goo<SetChartDataAction>
{
    public override string TypeName => "Set Chart Data Action";
    public override string TypeDescription => "Action to set chart data in Google Slides (placeholder)";

    public SetChartDataActionGoo() { }

    public SetChartDataActionGoo(SetChartDataAction value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "SetChartDataAction is null" : "";

    public override IGH_Goo Duplicate() => new SetChartDataActionGoo(Value);

    public override string ToString() => Value is null ? "null" : $"SetChartDataAction ({Value.SeriesData.Count} data points)";
}