using ArchSitePlan.Core.ModelEditActions.Configs;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    public class BuildingAction : IModelEditAction
    {
        public static void Run(List<BuildingActionConfig> configs, EditType editType)
        {
            switch (editType)
            {
                case EditType.Remove:
                    TryRemoveFromCoreModel(configs);
                    break;

                case EditType.Add:
                    TryRemoveFromCoreModel(configs);
                    TryAddToCoreModel(configs);
                    break;

                //case EditType.Edit:
                //    var building = CoreModelSingelton.InstanceBuildings.Find(b => b.EnvelopeId == envelopeId);
                //    if(building is not null)
                //        TryEditInCoreModel(building);
                //    break;

                default:
                    throw new NotImplementedException(nameof(ParcelAction) + $" Edit mode:{editType}");
            }
        }

        private static void TryAddToCoreModel(List<BuildingActionConfig> configs)
        {
            foreach (var config in configs)
            {
                var origin = config.EnvelopeMesh.Bounds().Centre().ProjectToXY();
                var nearest = CoreModelSingelton.Instance.Parcels
                    .FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([origin]));

                var use = nearest is not null ? nearest.LandUse : AppLanduses.Undefined;

                var newBldg = new Building(
                        config.EnvelopeMesh,
                        config.FloorPlates,
                        config.FlootToFloorHt,
                        config.EnvelopeId,
                        config.FootprintId);

                CoreModelSingelton.Instance.Buildings.Add(newBldg);
            }

        }

        private static bool TryRemoveFromCoreModel(List<BuildingActionConfig> configs)
        {
            var guids = configs.Select(c => c.EnvelopeId).ToList();
            int count = CoreModelSingelton.Instance.Buildings.RemoveAll(bldg => guids.Contains(bldg.EnvelopeId));
            return count > 0;
        }
    }
}
