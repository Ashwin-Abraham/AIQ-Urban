using NUnit.Framework;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Tests.Helpers;

internal static class RhinoDocAssertions
{
    public static void AssertLayerObjectCount(RhinoDoc doc, string layerPath, int expectedCount)
    {
        var layerIndex = doc.Layers.FindByFullPath(layerPath, -1);
        if (layerIndex < 0)
        {
            Assert.That(expectedCount, Is.EqualTo(0), $"Layer '{layerPath}' does not exist but expected {expectedCount} objects");
            return;
        }

        var actualCount = 0;
        foreach (var obj in doc.Objects)
        {
            if (obj != null && obj.Attributes.LayerIndex == layerIndex)
                actualCount++;
        }

        Assert.That(actualCount, Is.EqualTo(expectedCount), $"Expected {expectedCount} objects on layer '{layerPath}' but found {actualCount}");
    }
}
