using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.Drawing;
using AIQ.GH.Exports.Models;

namespace AIQ.GH.Exports.Types;

public class LayerPropertiesGoo : GH_Goo<LayerProperties>
{
    public override string TypeName => "Layer Properties";
    public override string TypeDescription => "Layer properties with visibility, color, line weight, and linetype";

    public LayerPropertiesGoo() { }

    public LayerPropertiesGoo(LayerProperties value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "LayerProperties is null" : "";

    public override IGH_Goo Duplicate() => new LayerPropertiesGoo(Value);

    public override string ToString() => Value?.FullPath ?? "null";
}
