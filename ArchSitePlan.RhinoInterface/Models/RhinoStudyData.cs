using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Models.CreationData;
using ArchSitePlan.RhinoBhomInterop;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.Models
{
    internal class RhinoStudyData
    {
        public Curve Curve;
        public Guid ID ;
        public string Name;

        private RhinoStudyData(Curve curve, Guid id, string name)
        {
            Curve = curve;
            ID = id;
            Name = name;
        }

        private static bool ValidateInput(RhinoObject obj)
        {
            return obj.Geometry is Curve rhinoCrv
                && rhinoCrv.IsClosed
                && rhinoCrv.Degree < 3;
        }

        public static RhinoStudyData? Create(RhinoObject obj, string name)
        {
            if (!ValidateInput(obj))
                return null;

            return new RhinoStudyData((obj.Geometry as Curve)!, obj.Id, name);
        }

        public static IEnumerable<StudyData> ToCoreModelData(IEnumerable<RhinoStudyData> rhinoData)
        {
            return rhinoData.Select(d => d.ToCoreModelData());
        }

        public StudyData ToCoreModelData()
        {
            return new StudyData(
                Curve.ToBHoM2DegreeCrv()!,
                ID,
                Name);
        }

        internal static List<RhinoStudyData> FromExisting(List<RhinoObject> rhinoObjects)
        {
            List<RhinoStudyData> dataObjects = new();
            var studyIDs = CoreModelSingelton.Instance.Zones.Select(s => s.Id).ToList();

            foreach (RhinoObject obj in rhinoObjects)
            {
                StudyArea? study = CoreModelSingelton.Instance.Zones.Find(s => s.Id == obj.Id);
                RhinoStudyData? data = null;
                
                if (study != null && ValidateInput(obj))
                    data = Create(obj, study.Name);
                
                if(data is not null)
                    dataObjects.Add(data);
            }

            return dataObjects;
        }
    }
}
