using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.GH.Exports.Types;

public class LayerConfigurationListGoo : GH_Goo<List<LayerConfiguration>>
{
    public override bool IsValid => Value is not null;

    public override string IsValidWhyNot => IsValid ? string.Empty : "Value is null";

    public override string TypeName => "LayerConfigurationList";

    public override string TypeDescription => "A list of layer configurations";

    public LayerConfigurationListGoo()
    {
        Value = [];
    }

    public LayerConfigurationListGoo(List<LayerConfiguration> value)
    {
        Value = value ?? [];
    }

    public LayerConfigurationListGoo(LayerConfigurationListGoo other)
    {
        Value = other.Value is null ? [] : new List<LayerConfiguration>(other.Value);
    }

    public override IGH_Goo Duplicate()
    {
        return new LayerConfigurationListGoo(this);
    }

    public override string ToString()
    {
        if (Value is null || Value.Count == 0)
            return "Empty Layer Configuration List";

        return $"{Value.Count} Layer Configuration{(Value.Count != 1 ? "s" : "")}";
    }

    public override bool CastTo<Q>(ref Q target)
    {
        if (typeof(Q) == typeof(List<LayerConfiguration>))
        {
            target = (Q)(object)Value;
            return true;
        }

        if (typeof(Q) == typeof(LayerConfiguration))
        {
            target = Value.Count > 0 ? (Q)(object)Value[0] : default;
            return Value.Count > 0;
        }

        target = default;
        return false;
    }

    public override bool CastFrom(object source)
    {
        if (source is List<LayerConfiguration> list)
        {
            Value = list;
            return true;
        }

        if (source is LayerConfiguration single)
        {
            Value = [single];
            return true;
        }

        if (source is IEnumerable<LayerConfiguration> enumerable)
        {
            Value = enumerable.ToList();
            return true;
        }

        return false;
    }
}
