using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIQ.GH.Exports.Types;

public class LayerConfigurationListGoo : GH_Goo<List<LayerConfiguration>>
{
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

    public override bool IsValid => Value is not null && Value.Count > 0;

    public override string IsValidWhyNot
    {
        get
        {
            if (Value is null) return "LayerConfigurationList is null";
            if (Value.Count == 0) return "LayerConfigurationList is empty";
            return string.Empty;
        }
    }

    public override IGH_Goo Duplicate()
    {
        return new LayerConfigurationListGoo(this);
    }

    public override string ToString()
    {
        if (Value is null || Value.Count == 0)
            return "Empty Layer Configuration List";

        var preview = string.Join(", ", Value.Take(3).Select(v => v.LayerName));
        return Value.Count > 3
            ? $"{Value.Count} layers: {preview}..."
            : $"{Value.Count} layer{(Value.Count != 1 ? "s" : "")}: {preview}";
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

    public override bool Write(GH_IWriter writer)
    {
        if (Value is null) return false;

        var sb = new StringBuilder();
        foreach (var config in Value)
        {
            sb.AppendLine(config.ToJson());
        }
        writer.SetString("data", sb.ToString());
        return true;
    }

    public override bool Read(GH_IReader reader)
    {
        var data = reader.GetString("data");
        if (string.IsNullOrEmpty(data))
        {
            Value = [];
            return true;
        }

        Value = [];
        var lines = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var config = LayerConfiguration.FromJson(line.Trim());
            if (config is not null)
            {
                Value.Add(config);
            }
        }
        return true;
    }
}
