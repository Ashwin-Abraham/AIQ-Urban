using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions.Configs;
using ArchSitePlan.Core.Models;
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

                case EditType.UpdateUse:
                    EditUse(configs);
                    break;

                //case EditType.Edit:
                //    var building = CoreModel.Buildings.Find(b => b.EnvelopeId == envelopeId);
                //    if(building is not null)
                //        TryEditInCoreModel(building);
                //    break;

                default:
                    throw new NotImplementedException(nameof(ParcelAction) + $" Edit mode:{editType}");
            }
        }

        private static void EditUse(List<BuildingActionConfig> configs)
        {
            var ids = configs.Select(c => c.EnvelopeId).ToList();
            var buildings = CoreModel.Buildings.Where(b => ids.Contains(b.EnvelopeId)).ToList();

            foreach ( var building in buildings)
            {
                var parcel = CoreModel.Parcels.Find(p => 
                    p.Boundary.ExternalBoundary.IIsContaining(building.Origin.ObjectToList()));

                building.Use = parcel is null ? Landuse.Undefined : parcel.LandUse;
            }
        }

        private static void TryAddToCoreModel(List<BuildingActionConfig> configs)
        {
            foreach (var config in configs)
            {
                var origin = config.EnvelopeMesh.Bounds().Centre().ProjectToXY();
                var nearest = CoreModel.Parcels
                    .FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining(origin.ObjectToList()));

                var use = nearest is not null ? nearest.LandUse : Landuse.Undefined;

                var newBldg = new Building(
                        config.EnvelopeMesh,
                        config.FloorPlates,
                        config.FlootToFloorHt,
                        config.EnvelopeId,
                        use,
                        config.FootprintId);

                CoreModel.Buildings.Add(newBldg);
            }

        }

        /// <summary>
        /// Edits the buildings host parcel id
        /// </summary>
        private static bool TryEditInCoreModel(Building building)
        {
            var parcel = CoreModel.Parcels
                .FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining(building.Origin.ObjectToList()));
            building.HostParcelId = parcel?.Id ?? Guid.Empty;

            return building.HostParcelId == Guid.Empty;
        }

        private static bool TryRemoveFromCoreModel(List<BuildingActionConfig> configs)
        {
            var guids = configs.Select(c => c.EnvelopeId).ToList();
            int count = CoreModel.Buildings.RemoveAll(bldg => guids.Contains(bldg.EnvelopeId));
            return count > 0;
        }
    }
}
