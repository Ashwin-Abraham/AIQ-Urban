using System.IO;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.OSM;
using ArchSitePlan.OSM.Models;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class ReadOsmMessage:IMessageFromUI<TextMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ReadOsm;

        [JsonProperty("filePath")]
        public string FilePath { get; init; }

        public TextMessage HandleMessage()
        {
            if(!OSMFileReader.TryReadFile(FilePath, out var osmModel))
                return new TextMessage($"File not found {FilePath}");

            var roads = osmModel.OsmObjects
                .Select(x => (x as IOsmPolyline)?.Centerline)
                .Where(x => x is not null)
                .Select(x => x.ToRhino())
                .ToList();

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            foreach(var road in roads)
                doc.Objects.AddCurve(road);

            return new TextMessage($"Added {roads.Count} roads to rhino");
        }
    }
}
