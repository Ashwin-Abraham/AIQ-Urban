using Rhino.DocObjects;
using System.Drawing;

namespace ArchSitePlan.RhinoInterface.Constants
{
    public static class RHGeneralConstants
    {
        public const string NamedViewPrefix = "AIQ_NamedView_";

        public static int MaxGraphBarSize = 200;

        public static List<ObjectType> BuildingFootprintObjectTypes = new List<ObjectType>(){ ObjectType.Curve};

        public static List<ObjectType> BuildingEnvelopeObjectTypes = new List<ObjectType>()
        { 
            ObjectType.Extrusion,
            ObjectType.Brep,
            ObjectType.Mesh
        };

        public static readonly Color DefaultColor = Color.Gray;
    }
}
