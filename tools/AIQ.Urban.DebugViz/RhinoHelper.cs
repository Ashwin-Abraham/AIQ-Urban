using System.Drawing;
using Rhino;

namespace AIQ.Urban.DebugViz;

public static class RhinoHelper
{
    public static int EnsureLayer(RhinoDoc doc, string path, Color color)
    {
        var idx = doc.Layers.FindByFullPath(path, -1);
        if (idx >= 0)
        {
            doc.Layers[idx].Color = color;
            return idx;
        }
        return doc.Layers.AddPath(path, color);
    }
}
