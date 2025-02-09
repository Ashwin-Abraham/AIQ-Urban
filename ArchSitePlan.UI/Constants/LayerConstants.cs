using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class LayerConstants
    {
        public const string MainModellingLayer = "AIQ Urban Model";
        public const string _Sep_ = "::";
        public static string StudyAreaLayer = $"{MainModellingLayer}{_Sep_}Study Area";
        public static string BuildingFootprintLayer = $"{BuildingLayer}{_Sep_}Footprint";
        public static string BuildingFloorOutlineLayer = $"{BuildingLayer}{_Sep_}Floor Plate Outline";
        public static string BuildingMassLayer = $"{BuildingLayer}{_Sep_}Mass";
        public const string BuildingLayer = $"{MainModellingLayer}{_Sep_}Buildings";
        public const string ParcelsLayer = $"{MainModellingLayer}{_Sep_}Parcels";
        public const string RoadLayer = $"{MainModellingLayer}{_Sep_}Roads";
        public const string RoadOutlineLayer = $"{RoadLayer}{_Sep_}Outline";
        public const string RoadCLLayer = $"{RoadLayer}{_Sep_}CL";
        public const string RoadROWLayer = $"{RoadLayer}{_Sep_}RoW";
        public const string ParcelDividerLayer = $"{MainModellingLayer}{_Sep_}ParcelDivider";
        public const int DefaultLayerIndex = -1;
        public static Color RoadColour = Color.Black;
        public static Color BuildingMassColour = Color.LightGray;
        public static Color BuildingFloorOutlineColour = Color.DarkGray;
        public static Color BuildingFootprintColour = Color.DarkRed;
        public static Color StudyAreaColour = Color.IndianRed;
        public static Color ParcelDividerColour = Color.DarkViolet;

        public const string MainOSMLayer = "AIQ Map Import";
        public const string RoadsOSMLayer = MainOSMLayer + _Sep_ + "Roads";
        public const string LanduseOSMLayer = MainOSMLayer + _Sep_ + "Landuse";
        public const string BuildingOSMLayer = MainOSMLayer + _Sep_ + "Buildings";
        public static Color DefaultLanduseColor = Color.Beige;

        public static Dictionary<string, Color> LanduseColorDefaults { get; } = new()
        {
            // Common landuse key values – developed land
            { "commercial", Color.FromArgb(255, 102, 102) },   // soft red
            { "construction", Color.Orange },
            { "education", Color.FromArgb(102, 153, 255) },      // light blue
            { "fairground", Color.MediumPurple },
            { "industrial", Color.Gray },
            { "residential", Color.Yellow },                    // bright yellow per LBCS
            { "retail", Color.Red },
            { "institutional", Color.Navy },

            // Common landuse key values – rural and agricultural land
            { "aquaculture", Color.Teal },
            { "allotments", Color.LightGreen },
            { "farmland", Color.Green },
            { "farmyard", Color.SaddleBrown },
            { "paddy", Color.Olive },
            { "animal_keeping", Color.Sienna },
            { "flowerbed", Color.Pink },
            { "forest", Color.DarkGreen },
            { "logging", Color.Brown },
            { "greenhouse_horticulture", Color.Lime },
            { "meadow", Color.LightGreen },
            { "orchard", Color.Orange },
            { "plant_nursery", Color.LightGreen },
            { "vineyard", Color.MediumOrchid },
            { "basin", Color.CornflowerBlue },
            { "reservoir", Color.RoyalBlue },  // special handling may apply
            { "salt_pond", Color.LightBlue },

            // Other landuse key values
            { "brownfield", Color.Peru },
            { "cemetery", Color.DarkGray },
            { "conservation", Color.ForestGreen },
            { "depot", Color.DimGray },
            { "garages", Color.DimGray },
            { "grass", Color.Green },
            { "greenfield", Color.ForestGreen },
            { "landfill", Color.Maroon },
            { "military", Color.OliveDrab },
            { "port", Color.MidnightBlue },
            { "quarry", Color.SlateGray },
            { "railway", Color.Black },
            { "recreation_ground", Color.LightSeaGreen },
            { "religious", Color.MediumVioletRed },
            { "village_green", Color.GreenYellow },
            { "greenery", Color.ForestGreen },
            { "winter_sports", Color.SkyBlue },
            { "user defined", Color.Beige }
        };
    }
}
