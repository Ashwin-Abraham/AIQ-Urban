using System.Drawing;
using ArchSitePlan.OSM;
using ArchSitePlan.OSM.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class ReadOsmMessage:IMessageFromUI<TextMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ReadOsm;

        [JsonProperty("filePath")]
        public string FilePath { get; init; }

        public TextMessage HandleMessage()
        {
            int roadCount = 0, parcelCount = 0;
            if(!OSMFileReader.TryReadFile(FilePath, out var osmModel)
                || osmModel is null)
                return new TextMessage($"File not found {FilePath}");

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            foreach(var road in osmModel.Highways)
            {
                var curve = road.Centerline.ToRhino();
                var attributes = GetObjectAttributes(road, doc);
                doc.Objects.AddCurve(curve, attributes);
                roadCount++;
            }

            foreach(var parcel in osmModel.LandParcels)
            {
                var surface = Create.PlanarSurface(parcel.Boundary).ToRhino();
                if (surface is null) continue;

                var attributes = GetObjectAttributes(parcel, doc);
                doc.Objects.AddBrep(surface, attributes);
            }

            double defaultStoreyHeight = 3.5;
            int defaultLevels = 4;

            foreach (var building in osmModel.Buildings)
            {
                double height;

                if (building.Height.HasValue)
                    height = building.Height.Value;
                else if (building.Levels.HasValue)
                    height = building.Levels.Value * defaultStoreyHeight;
                else
                    height = defaultStoreyHeight * defaultLevels;

                var planarSurface = building.Boundary.ToRhino();
                if (planarSurface is null) continue;

                var attributes = GetObjectAttributes(building, doc);
                doc.Objects.AddExtrusion(
                    Extrusion.Create(planarSurface, Plane.WorldXY, height, true),
                    attributes);
            }

            doc.Views.Redraw();
            return new TextMessage($"Added {roadCount} roads to rhino");
        }

        private ObjectAttributes GetObjectAttributes(LandParcel parcel, RhinoDoc doc)
        {
            var layerName =
                LayerConstants.LanduseOSMLayer + LayerConstants._Sep_
                + parcel.Type;


            Color color = LayerConstants.DefaultLanduseColor;
            if (LayerConstants.LanduseColorDefaults.TryGetValue(parcel.Type, out var actualColor))
                color = actualColor;

            int layerIndex = LayerHelpers.CreateOrGetLayerIndex(
                doc, 
                layerName,
                color);

            return new ObjectAttributes() 
            { 
                LayerIndex = layerIndex 
            };
        }
            
        private ObjectAttributes GetObjectAttributes(Building _, RhinoDoc doc)
        {
            var layerName = LayerConstants.BuildingOSMLayer;

            int layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, layerName);
            return new ObjectAttributes() { LayerIndex = layerIndex };
        }

        private ObjectAttributes GetObjectAttributes(Highway highway, RhinoDoc doc)
        {
            string width = highway.Width.HasValue ? 
                highway.Width.Value.ToString() 
                : "Undefined Width";

            var layerName = 
                LayerConstants.RoadsOSMLayer + LayerConstants._Sep_
                +highway.Type + LayerConstants._Sep_ 
                + width;

            int layerIndex = LayerHelpers.CreateOrGetLayerIndex( doc,layerName);
            return new ObjectAttributes() { LayerIndex = layerIndex };
        }
    }
}
