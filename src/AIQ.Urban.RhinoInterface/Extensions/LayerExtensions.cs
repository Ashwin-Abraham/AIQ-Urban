using Rhino.DocObjects;

namespace AIQ.Urban.RhinoInterface.Extensions;
public static class LayerExtensions
{
    public static List<Layer> GetLeafChildren(this Layer layer)
    {
        var children = layer.GetChildren(true);

        if (children.Length == 0)
            return [layer];

        var leaves = new List<Layer>();
        foreach(var child in children)
        {
            if(child.GetChildren()?.Length == 0)
                leaves.Add(layer);
        }

        return leaves;
    }
}
