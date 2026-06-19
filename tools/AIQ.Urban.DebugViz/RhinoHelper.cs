using System.Drawing;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.DebugViz;

public static class RhinoHelper
{
    private static int EnsureLayer(RhinoDoc doc, string path, Color color)
    {
        var idx = doc.Layers.FindByFullPath(path, -1);
        if (idx >= 0)
        {
            doc.Layers[idx].Color = color;
            return idx;
        }
        return doc.Layers.AddPath(path, color);
    }

    public static ObjectAttributes MakeAttributes(RhinoDoc doc, string layerPath, Color layerColor, Color? objectColor = null)
    {
        var layerIndex = EnsureLayer(doc, layerPath, layerColor);
        var attributes = new ObjectAttributes { LayerIndex = layerIndex };

        if (objectColor.HasValue)
        {
            attributes.ColorSource = ObjectColorSource.ColorFromObject;
            attributes.ObjectColor = objectColor.Value;
        }

        return attributes;
    }
}
