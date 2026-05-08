using System.IO;
using ArchSitePlan.OSM;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using ArchSitePlan.RhinoInterface.RhinoOSM;
using ArchSitePlan.RhinoInterface.RhinoOSM.Areas;
using ArchSitePlan.RhinoInterface.RhinoOSM.Polylines;
using Newtonsoft.Json;
using Rhino;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
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

            // Save current osm file in new location
            if(!File.Exists(FilePath))
                return new TextMessage($"File not found {FilePath}");

            var fileName = Path.GetFileName(FilePath);
            string filePath = Path.Combine(OSMConstants.OsmFileDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            File.Copy(FilePath, filePath);

            if (!OSMFileReader.TryMakeOSMModelFromFile(FilePath, out var osmModel)
                || osmModel is null)
                return new TextMessage($"File not found {FilePath}");

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            // Save the object IDs into a singleton list
            var idStore = OSMConstants.OsmIdsByFile;
            osmModel.GetModelObjects()
                .ForEach(x => idStore.TryAdd(x.Id.GetValueOrDefault(), filePath));

            // Closed ways
            foreach (var parcel in osmModel.LandParcels)
            {
                var guid = new LandParcelRhOsm(parcel, doc).AddToDocument();
                if (guid == Guid.Empty) continue;
                
                parcelCount++;
            }

            foreach (var building in osmModel.Buildings)
            {
                var guid = new BuildingRhOsm(building, doc) .AddToDocument();
                if (guid != Guid.Empty) continue;

                buildingCount++;
            }

            foreach(var natural in osmModel.NaturalAreas)
            {
                var guid = new NaturalClosedRhOsm(natural, doc).AddToDocument();
                if (guid != Guid.Empty) naturalClosedCount++;
            }

            foreach (var misc in osmModel.MiscAreas)
            {
                var guid = new MiscRhOsm(misc, doc).AddToDocument();
                if (guid != Guid.Empty) miscCount++;
            }

            // Open ways
            foreach (var road in osmModel.Highways)
            {
                var guid = new TransitRhOsm(road, doc).AddToDocument();
                if (guid != Guid.Empty) roadCount++;
            }

            foreach ( var natural in osmModel.NaturalOpen)
            {
                var guid = new NaturalOpenRhOsm(natural, doc).AddToDocument();
                if (guid != Guid.Empty) naturalOpenCount++;
            }

            foreach (var power in osmModel.Power)
            {
                var guid = new PowerRhOsm(power, doc).AddToDocument();
                if (guid != Guid.Empty) powerCount++;
            }

            foreach (var boundary in osmModel.BoundaryBarrier)
            {
                var guid = new BoundaryBarrierRhOsm(boundary, doc).AddToDocument();
                if (guid != Guid.Empty) boundaryBarrierCount++;
            }

            foreach (var waterway in osmModel.Waterways)
            {
                var guid = new WaterwayRhOsm(waterway, doc).AddToDocument();
                if (guid != Guid.Empty) boundaryBarrierCount++;
            }

            doc.Views.EnableRedraw(true, true, true);
            doc.Views.Redraw();
            return new TextMessage($"Added {roadCount} roads to rhino");
        }
    }
}