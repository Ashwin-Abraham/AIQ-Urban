using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace AIQ.GH.Exports.Types;

public class LayerConfigurationGoo : GH_Goo<LayerConfiguration>
{
    public override string TypeName => "Layer Configuration";
    public override string TypeDescription => "A layer configuration with visibility, color, and lock state";

    public LayerConfigurationGoo() { }

    public LayerConfigurationGoo(LayerConfiguration value) => Value = value;

    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "LayerConfiguration is null" : "";

    public override IGH_Goo Duplicate() => new LayerConfigurationGoo(Value);

    public override string ToString() => Value?.LayerName ?? "null";
}
