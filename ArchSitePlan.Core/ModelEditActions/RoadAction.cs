using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    internal class RoadAction:IModelEditAction
    {
        internal static void Run(List<(ICurve? centerLine, Guid id)> centerLineAndIds, 
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
            }
        }

        internal static void Run(List<PlanarSurface> RoWs, EditType editType = EditType.Add)
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
