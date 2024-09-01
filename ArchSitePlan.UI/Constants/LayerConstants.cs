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
        private const string _sep_ = "::";
        public static string StudyAreaLayer = $"{MainLayer}{_sep_}Study Area";
        public static string BuildingFloorOutlineLayer = $"{BuildingLayer}{_sep_}Floor Plate Outline";
        public static string BuildingMassLayer = $"{BuildingLayer}{_sep_}Mass";
        public const string BuildingLayer = $"{MainLayer}{_sep_}Buildings";
        public const string ParcelsLayer = $"{MainLayer}{_sep_}Parcels";
        public const string RoadLayer = $"{MainLayer}{_sep_}Roads";
        public const string RoadCLLayer = $"{RoadLayer}{_sep_}CL";
        public const string RoadROWLayer = $"{RoadLayer}{_sep_}RoW";
        public const int DefaultLayerIndex = -1;
        public static Color RoadColour = Color.DarkGray;
        public static Color BuildingMassColour = Color.LightGray;
        public static Color BuildingFloorOutlineColour = Color.Black;
        public static Color StudyAreaColour = Color.IndianRed;
    }
}
