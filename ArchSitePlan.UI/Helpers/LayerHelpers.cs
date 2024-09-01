using ArchSitePlan.UI.Constants;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.UI.Helpers
{
    public static class LayerHelpers
    {
        public static void EnsureLayerExists(RhinoDoc doc, string layerName)
        {
            int result = doc.Layers.FindByFullPath(layerName, -1);

            if(result == -1)
                doc.Layers.AddPath(layerName);
        }

        public static int CreateOrGetLayerIndex(RhinoDoc doc,
            string layerName,
            Color layerColor)
        {
            string layer = layerName;
            int index = doc.Layers.FindByFullPath(layer, LayerConstants.DefaultLayerIndex);
            
            if (index == LayerConstants.DefaultLayerIndex)
                index = doc.Layers.AddPath(layer);

            doc.Layers[index].Color = layerColor;
            return index;
        }

        public static void ToggleLockLayer(RhinoDoc doc, string layerName, bool lockLayer)
        {
            string layer = layerName;
            int index = doc.Layers.FindByFullPath(layer, LayerConstants.DefaultLayerIndex);

            if (index != LayerConstants.DefaultLayerIndex)
                doc.Layers[index].IsLocked = lockLayer;
        }
        public static string GetParcelRhinoLayerName(Landuse use)
        {
            return LayerConstants.ParcelsLayer + "::" + use.ToString();
        }

        internal static void SetLayerColour(RhinoDoc doc, int layerIndex, Color color)
        {
            doc.Layers[layerIndex].Color = color;
        }

        public static void ChangeLayer(RhinoObject obj, int layerIndex)
        {
            obj.Attributes.LayerIndex = layerIndex;
            obj.CommitChanges();
        }
    }

}
