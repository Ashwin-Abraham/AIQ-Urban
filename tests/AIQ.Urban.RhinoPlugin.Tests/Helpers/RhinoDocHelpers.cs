using System.Collections.Generic;
using System.Linq;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Tests.Helpers;
internal static class RhinoDocHelpers
{
    internal static List<(string Name, int Index, int Count)> GetObjectCountByLayer(RhinoDoc doc)
    {
        var layerNameAndIndex = doc.Layers.AsEnumerable()
            .Select(x => (x.FullPath, x.Index))
            .ToList();

        List<(string Name, int Index, int Count)> outputList = layerNameAndIndex.Select(x => (x.FullPath, x.Index, 0)).ToList();

        foreach (var obj in doc.Objects)
        {
            var index = obj.Attributes.LayerIndex;

            var item = outputList.Find(x => x.Index == index);

            if (item == default)
                continue;

            outputList.Remove(item);
            var count = item.Count + 1;
            outputList.Add((item.Name, item.Index, count));
        }

        return outputList;
    }

}
