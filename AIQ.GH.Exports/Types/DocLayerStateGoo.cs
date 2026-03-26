using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Models;

namespace AIQ.GH.Exports.Types;

public class DocLayerStateGoo : GH_Goo<DocLayerState>
{
    public override string TypeName => "Doc Layer State";
    public override string TypeDescription => "Document layer state capturing layer properties";

    public DocLayerStateGoo() { }

    public DocLayerStateGoo(DocLayerState value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "DocLayerState is null" : "";

    public override IGH_Goo Duplicate() => new DocLayerStateGoo(Value);

    public override string ToString() => Value is null ? "null" : $"DocLayerState ({Value.LayerState.Count} layers)";
}
