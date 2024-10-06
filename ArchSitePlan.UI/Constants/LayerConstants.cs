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
        public const string MainLayer = "ArchSitePlan";
        public const string _Sep_ = "::";
        public static string StudyAreaLayer = $"{MainLayer}{_Sep_}Study Area";
        public static string BuildingFootprintLayer = $"{BuildingLayer}{_Sep_}Footprint";
        public static string BuildingFloorOutlineLayer = $"{BuildingLayer}{_Sep_}Floor Plate Outline";
        public static string BuildingMassLayer = $"{BuildingLayer}{_Sep_}Mass";
        public const string BuildingLayer = $"{MainLayer}{_Sep_}Buildings";
        public const string ParcelsLayer = $"{MainLayer}{_Sep_}Parcels";
        public const string RoadLayer = $"{MainLayer}{_Sep_}Roads";
        public const string RoadOutlineLayer = $"{RoadLayer}{_Sep_}Outline";
        public const string RoadCLLayer = $"{RoadLayer}{_Sep_}CL";
        public const string RoadROWLayer = $"{RoadLayer}{_Sep_}RoW";
        public const string ParcelDividerLayer = $"{MainLayer}{_Sep_}ParcelDivider";
        public const int DefaultLayerIndex = -1;
        public static Color RoadColour = Color.DarkGray;
        public static Color BuildingMassColour = Color.LightGray;
        public static Color BuildingFloorOutlineColour = Color.Black;
        public static Color BuildingFootprintColour = Color.DarkRed;
        public static Color StudyAreaColour = Color.IndianRed;
        public static Color ParcelDividerColour = Color.DarkViolet;
    }
}
