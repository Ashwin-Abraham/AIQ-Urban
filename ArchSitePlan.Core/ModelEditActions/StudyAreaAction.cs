using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    public class StudyAreaAction:IModelEditAction
    {
        public static void Run(
            List<(ICurve? ICurve, Guid Id, string Name)> studyData,
           EditType editType)
        {
            switch (editType)
            {
                case EditType.Remove:
                    RemoveFromCoreModel(studyData.Select(s => s.Id).ToList());
                    break;

                case EditType.Add or EditType.Edit:
                    RemoveFromCoreModel(studyData.Select(s => s.Id).ToList());
                    AddToCoreModel(studyData);
                    break;

                default:
                    throw new NotImplementedException(nameof(StudyAreaAction) + $" Edit mode:{editType}");
            }
        }

        private static void AddToCoreModel(List<(ICurve Boundary, Guid Id, string Name)> studyData)
        {
            var newStudies = new List<StudyArea>();

            foreach(var data  in studyData)
            {
                var parcelsWithin = CoreModelSingelton.Instance.Parcels
                    .Where(p => data.Boundary.IIsContaining([p.Boundary.ControlPointCentroid()]))
                    .ToList();

                var buildingsWithin = CoreModelSingelton.Instance.Buildings
                    .Where(bldg => data.Boundary.IIsContaining([bldg.Origin]))
                    .ToList();

                var roadsWithin = CoreModelSingelton.Instance.Roads
                    .Where(r => data.Boundary.IIsContaining([r.Centerline.PointAtParameter(0.5)]))
                    .ToList();

                var study = new StudyArea(data.Boundary,
                    data.Id,
                    data.Name,
                    parcelsWithin,
                    buildingsWithin,
                    roadsWithin);

                newStudies.Add(study);
            }

            CoreModelSingelton.Instance.Studies.AddRange(newStudies);
        }

        private static void RemoveFromCoreModel(List<Guid> ids)
        {
            int removed = CoreModelSingelton.Instance.Studies.RemoveAll(study => ids.Contains(study.Id));
        }
    }
}
