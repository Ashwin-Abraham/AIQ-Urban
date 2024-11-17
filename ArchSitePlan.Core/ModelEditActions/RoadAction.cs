using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    internal class RoadAction:IModelEditAction
    {
        internal static void Run(
            List<(ICurve? centerLine, Guid id)> centerLineAndIds, 
            EditType editType, 
            double? roadWidth)
        {
            switch(editType)
            {
                case EditType.Remove:
                    RemoveFromCoreModel(centerLineAndIds);
                    break;

                case EditType.Edit or EditType.Add:
                    RemoveFromCoreModel(centerLineAndIds);
                    AddToCoreModel(centerLineAndIds, (double)roadWidth);
                    break;

                case EditType.Divide:
                    var dividers = GetCoreModelSplitters();
                    dividers.AddRange(dividers);
                    DivideRoads(centerLineAndIds, dividers);
                    break;

            }
        }
        private static List<ICurve> GetCoreModelSplitters()
        {
            var dividers = CoreModel.Dividers.Select(d => d.Curve as ICurve).ToList();
            var studyDividers = CoreModel
                .Studies
                .Where(s => s.Boundary != null)
                .Select(x => (x.Boundary as ICurve)!)
                .ToList();

            dividers.AddRange(studyDividers);

            return dividers;
        }
        private static void DivideRoads(List<(ICurve? centerLine, Guid id)> centerLineAndIds,
            List<ICurve> splitters)
        {
            var ids = centerLineAndIds.Select(x => x.id).ToList();
            var roadsToDiv = CoreModel.Roads.Where(rd => ids.Contains(rd.CenterLineId)).ToList();

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

            var undividedRoads = CoreModel.Roads
                .Where(rd => !roadsToDiv.Contains(rd))
                .ToList();

            CoreModel.Roads = undividedRoads.Concat(newRoads).ToList();
        }

        internal static void Run(List<PlanarSurface> RoWs, 
            EditType editType = EditType.Add)
        {
            switch(editType)
            {
                case EditType.Add:
                    if (RoWs == null || RoWs.Count == 0) return;
                    CoreModel.ROWs = RoWs;
                    return;

                case EditType.ClearAll:
                    CoreModel.ROWs.Clear();
                    return;

                case EditType.Divide:
                    var dividers = GetCoreModelSplitters();
                    CoreModel.ROWs =  RoWs
                        .SelectMany(r => r.Split(dividers))
                        .ToList();
                    return;

                default: 
                    throw new NotImplementedException($"{nameof(RoadAction)} of {editType.ToString()}  not implemented");
            }
        }

        private static void AddToCoreModel(
            List<(ICurve centerLine, Guid id)> centerLineAndIds, 
            double width)
        {
            foreach(var clAndId in centerLineAndIds)
            {
                var pCurve = Create.PolyCurve(clAndId.centerLine.ObjectToList());
                if (Road.TryCreateRoad(pCurve, clAndId.id, width, out Road? road))
                    CoreModel.Roads.Add(road!);
            }
        }

        private static void RemoveFromCoreModel(
            List<(ICurve? centerLine, Guid id)> centerLineAndIds)
        {
            var ids = centerLineAndIds.Select(x => x.id).ToList();
            CoreModel.Roads.RemoveAll(r => ids.Contains(r.CenterLineId));
        }
    }
}
