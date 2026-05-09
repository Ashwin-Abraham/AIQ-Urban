using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

namespace AIQ.Urban.RhinoPlugin.Extensions;

/// <summary>Extension methods for Rhino <see cref="LayerTable"/>.</summary>
internal static class LayerTableExtensions
{
    /// <summary>Ensures a layer with the given full path exists, creating parent layers as needed.</summary>
    /// <param name="layers">The document layer table.</param>
    /// <param name="fullPath">Full layer path using :: separator, e.g. "AIQ Urban::Transit".</param>
    /// <returns>Index of the layer.</returns>
    public static Layer EnsureLayerExists(this LayerTable layers, string fullPath)
    {
        var index = layers.FindByFullPath(fullPath, -1);
        
        if (index == -1)
            layers.AddPath(fullPath);
        
        return layers[index];
    }
}
