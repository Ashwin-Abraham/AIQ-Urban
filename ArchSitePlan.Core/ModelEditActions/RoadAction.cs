using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    internal class RoadAction:IModelEditAction
    {
        internal static void Run(
            List<TransitData> transitData, 
            EditType editType)
        {
            switch(editType)
            {
                case EditType.Remove:
                    RemoveFromCoreModel(transitData);
                    break;

                case EditType.Edit or EditType.Add:
                    RemoveFromCoreModel(transitData);
                    AddToCoreModel(transitData);
                    break;

                case EditType.Divide:
                    var dividers = GetCoreModelSplitters();
                    dividers.AddRange(dividers);
                    DivideRoads(transitData, dividers);
                    break;

            }
        }
        private static List<ICurve> GetCoreModelSplitters()
        {
            var dividers = CoreModelSingelton.Instance.Dividers.Select(d => d.Curve as ICurve).ToList();
            var studyDividers = CoreModelSingelton.Instance
                .Studies
                .Where(s => s.Boundary != null)
                .Select(x => (x.Boundary as ICurve)!)
                .ToList();

            dividers.AddRange(studyDividers);

            return dividers;
        }
        private static void DivideRoads(List<TransitData> roadData,
            List<ICurve> splitters)
        {
            var ids = roadData.Select(x => x.ID).ToList();
            var roadsToDiv = CoreModelSingelton.Instance.Roads.Where(rd => ids.Contains(rd.CenterLineId)).ToList();

            List<Road> newRoads = new List<Road>();

            foreach(var road in roadsToDiv)
            {
                var splitCLs = road.Centerline
                    .Split(splitters)
                    .Select(crv => crv.ToPolycurve());
                var splitRoads = splitCLs.Select(cl => new Road(cl, road))
                    .ToList();

                newRoads.AddRange(splitRoads);
            }

            var undividedRoads = CoreModelSingelton.Instance.Roads
                .Where(rd => !roadsToDiv.Contains(rd))
                .ToList();

            CoreModelSingelton.Instance.Roads = undividedRoads.Concat(newRoads).ToList();
        }

        internal static void Run(List<PlanarSurface> RoWs, 
            EditType editType = EditType.Add)
        {
            switch(editType)
            {
                case EditType.Add:
                    if (RoWs == null || RoWs.Count == 0) return;
                    CoreModelSingelton.Instance.ROWs = RoWs;
                    return;

                case EditType.ClearAll:
                    CoreModelSingelton.Instance.ROWs.Clear();
                    return;

                case EditType.Divide:
                    var dividers = GetCoreModelSplitters();
                    CoreModelSingelton.Instance.ROWs =  RoWs
                        .SelectMany(r => r.Split(dividers))
                        .ToList();
                    return;

                default: 
                    throw new NotImplementedException($"{nameof(RoadAction)} of {editType.ToString()}  not implemented");
            }
        }

        private static void AddToCoreModel(List<TransitData> roadData)
        {
            foreach(var data in roadData)
            {
                var pCurve = Create.PolyCurve([data.Centerline]);
                if (Road.TryCreateRoad(pCurve, data.ID, data.RoadWidth, out Road? road))
                    CoreModelSingelton.Instance.Roads.Add(road!);
            }
        }

        private static void RemoveFromCoreModel(
            List<TransitData> roadData)
        {
            var ids = roadData.Select(x => x.ID).ToList();
            CoreModelSingelton.Instance.Roads.RemoveAll(r => ids.Contains(r.CenterLineId));
        }
    }
}
