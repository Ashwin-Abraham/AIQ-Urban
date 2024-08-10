using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class LayerConstants
    {
        public const string MainLayer = "ArchSitePlan";
        private const string _sep_ = "::";
        public const string ParcelsLayer = $"{MainLayer}{_sep_}Parcels";
        public const string RoadLayer = $"{MainLayer}{_sep_}Roads";
        public const string RoadCLLayer = $"{RoadLayer}{_sep_}CL";
        public const string RoadROWLayer = $"{RoadLayer}{_sep_}RoW";
        public const int DefaultLayerIndex = -1;
    }
}
