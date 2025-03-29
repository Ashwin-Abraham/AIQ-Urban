using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.ModelEditActions.Configs;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.ModelEditActions
{
    public class AutoBuildingAction : IModelEditAction
    {
        public static void Run(List<AutoBuildingData> configs)
        {
            foreach(var config in configs)
            {
                if (config is AutoRectangleBuildingData rectConfig)
                {
                   // var building = RectangleBuilding.Generate(rectConfig);
                    //CoreModel.Buildings.Add(building);
                }
            }

        }

        private static void EditUse(List<BuildingActionConfig> configs)
        {
            var ids = configs.Select(c => c.EnvelopeId).ToList();
            var buildings = CoreModel.Buildings.Where(b => ids.Contains(b.EnvelopeId)).ToList();

            foreach ( var building in buildings)
            {
                var parcel = CoreModel.Parcels.Find(p => 
                    p.Boundary.ExternalBoundary.IIsContaining([building.Origin]));

                building.Use = parcel is null ? AppLanduses.Undefined : parcel.LandUse;
            }
        }

        private static void TryAddToCoreModel(List<BuildingActionConfig> configs)
        {
            foreach (var config in configs)
            {
                var origin = config.EnvelopeMesh.Bounds().Centre().ProjectToXY();
                var nearest = CoreModel.Parcels
                    .FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([origin]));

                var use = nearest is not null ? nearest.LandUse : AppLanduses.Undefined;

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
                .FirstOrDefault(p => p.Boundary.ExternalBoundary.IIsContaining([building.Origin]));
            building.HostParcelCoreModelId = parcel?.CoreModelId ?? Guid.Empty;

            return building.HostParcelCoreModelId == Guid.Empty;
        }

        private static bool TryRemoveFromCoreModel(List<BuildingActionConfig> configs)
        {
            var guids = configs.Select(c => c.EnvelopeId).ToList();
            int count = CoreModel.Buildings.RemoveAll(bldg => guids.Contains(bldg.EnvelopeId));
            return count > 0;
        }
    }
}
