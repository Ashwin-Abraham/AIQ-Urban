using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

namespace AIQ.Urban.RhinoInterface.Extensions;

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
            index = layers.AddPath(fullPath);
        
        return layers[index];
    }

    public static void PurgeEmpty(
        this LayerTable layers,
        RhinoDoc doc,
        string rootLayerName)
    {
        var rootLayerIndex = layers.FindByFullPath(rootLayerName, -1);

        if (rootLayerIndex == -1)
            return;

        var layerIndicesWithObjects = doc
            .Objects.Select(x => x.Attributes.LayerIndex)
            .Distinct();

        var rootLayer = layers[rootLayerIndex];

        // Start with leaves and work upwards
        var leaves = rootLayer.GetLeafChildren();

        while(leaves.Count > 0)
        {
            List<Layer> upperLevelLeaves = [];

            foreach (var leaf in leaves)
            {
                bool hasObject = layerIndicesWithObjects.Contains(leaf.Index);

                if (hasObject)
                    continue;

                var parent = leaf.ParentLayer(false);

                // Deletion only happens on empty leaf layers
                // If it has a child or an object it fails silently
                // Failure means we need to stop trying to delete it and its parents
                if (!layers.Delete(leaf, true))
                    continue;

                if (parent != null)
                    upperLevelLeaves.Add(parent);
            }

            leaves = upperLevelLeaves.Distinct().ToList();
        }
    }
}
