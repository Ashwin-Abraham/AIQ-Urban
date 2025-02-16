using ArchSitePlan.OSM;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.RhinoOSM;
using ArchSitePlan.UI.RhinoOSM.Areas;
using ArchSitePlan.UI.RhinoOSM.Polylines;
using Newtonsoft.Json;
using Rhino;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class ReadOsmMessage:IMessageFromUI<TextMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ReadOsm;

        [JsonProperty("filePath")]
        public string FilePath { get; init; }

        public TextMessage HandleMessage()
        {
            int roadCount = 0, 
                parcelCount = 0, 
                buildingCount = 0, 
                naturalClosedCount = 0,
                miscCount = 0,
                naturalOpenCount = 0,
                powerCount = 0,
                boundaryBarrierCount = 0;

            if(!OSMFileReader.TryReadFile(FilePath, out var osmModel)
                || osmModel is null)
                return new TextMessage($"File not found {FilePath}");

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Closed ways
            foreach(var parcel in osmModel.LandParcels)
            {
                var added = new LandParcelRhOsm(parcel, doc).AddToDocument();
                if (added) parcelCount++;
            }

            var buildingAttrib = BuildingRhOsm.GetObjectAttributes(doc);
            foreach (var building in osmModel.Buildings)
            {
                var added = new BuildingRhOsm(building, doc, buildingAttrib)
                    .AddToDocument();
                if (added) buildingCount++;
            }

            foreach(var natural in osmModel.NaturalAreas)
            {
                var added = new NaturalClosedRhOsm(natural, doc).AddToDocument();
                if (added) naturalClosedCount++;
            }

            foreach (var misc in osmModel.MiscAreas)
            {
                var added = new MiscRhOsm(misc, doc).AddToDocument();
                if (added) miscCount++;
            }

            // Open ways
            foreach (var road in osmModel.Highways)
            {
                bool added = new TransitRhOsm
                    (road, doc).AddToDocument();
                if (added) roadCount++;
            }

            foreach ( var natural in osmModel.NaturalOpen)
            {
                bool added = new NaturalOpenRhOsm(natural, doc).AddToDocument();
                if (added) naturalOpenCount++;
            }

            foreach (var power in osmModel.Power)
            {
                bool added = new PowerRhOsm(power, doc).AddToDocument();
                if (added) powerCount++;
            }

            foreach (var boundary in osmModel.BoundaryBarrier)
            {
                bool added = new BoundaryBarrierRhOsm(boundary, doc).AddToDocument();
                if (added) boundaryBarrierCount++;
            }

            foreach (var waterway in osmModel.Waterways)
            {
                bool added = new WaterwayRhOsm(waterway, doc).AddToDocument();
                if (added) boundaryBarrierCount++;
            }

            doc.Views.EnableRedraw(true, true, true);
            doc.Views.Redraw();
            return new TextMessage($"Added {roadCount} roads to rhino");
        }
    }
}
