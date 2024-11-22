using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class UIMessages
    {
        public static string SelectRoadCrv = "Select a line, poyline or arc.";
        public static string DrawLine = "Draw a line";
        public static string DrawPolyline = "Draw a polyline";

        public static string RoadInstruction = 
            "Select a centerline (a line, arc or polyline) and a road will be generated." +
            "\n Self intersecting curves are not accepted";

        public static string ChangeLandUseInstruction = "Select a landuse parcel and its use will be updated" +
            "\nLanduse parcels are created when roads or study areas create enclosed regions.";
        public static string AddDividerInstruction = "Select an arc, line or polyline and it will split all roads and parcels";
        public static string AddBuildingInstruction = "Select a footprint curve or 3D closed object and it will be made a building. Already generated buildings will have its size edited";
        public static string AddStudyInstruction = "Select ONE closed curve (made of lines and arcs) and it will be used as the boundary for a study area ";
        public static string SetExcelPathInstruction = "Select a location for excel output";
        public static string ExportExcelInstruction = "Export an excel report of the current project";

    }
}
