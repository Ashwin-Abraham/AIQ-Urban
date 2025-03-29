using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    public class ParcelAction:IModelEditAction
    {
        public void Run(
            EditType editType,
            List<ICurve>? boundariesToAdd = null,
            List<Guid>? clientIdsToModifyOrRemove = null,
            // use is only used during EditType.Edit
            Landuse? use= null,
            // dividers only used during EditType.Divide
            List<ICurve>? dividers = null,
            // existingUsesAndCenters only used during EditType.Add
            List<(Landuse, Point)>? existingUsesAndCenters = null)
        {
            use ??= AppLanduses.Undefined;
            dividers ??= new();
            existingUsesAndCenters ??= new();

            switch (editType)
            {
                case EditType.Remove:
                    if(clientIdsToModifyOrRemove != null)
                        RemoveFromCoreModel(clientIdsToModifyOrRemove);
                    break;

                case EditType.Add:
                    if (clientIdsToModifyOrRemove != null 
                        && boundariesToAdd != null)
                        RemoveFromCoreModel(clientIdsToModifyOrRemove);
                        AddToCoreModel(boundariesToAdd!, existingUsesAndCenters);
                    break;

                case EditType.Divide:
                    if (clientIdsToModifyOrRemove != null
                        && dividers != null)
                        DivideInCoreModel(clientIdsToModifyOrRemove, dividers);
                    break;

                case EditType.Edit:
                    if(clientIdsToModifyOrRemove != null)
                        ModifyLanduse(clientIdsToModifyOrRemove, use);
                    break;

                default: 
                    throw new NotImplementedException(nameof(ParcelAction) + $" Edit mode:{editType}" );
            }
        }


        private static void ModifyLanduse(List<Guid> clientModelIDs, Landuse use)
        {
            CoreModel.Parcels.Where(p => clientModelIDs.Contains(p.ClientModelId))
                .ToList()
                .ForEach(p => p.LandUse = use);
        }

        private static void DivideInCoreModel(
            List<Guid> clientIds,
            List<ICurve> dividers)
        {
            var parcels = CoreModel.Parcels.Where(x => clientIds.Contains(x.ClientModelId)).ToList();

            List<ICurve> cleanedDividers = dividers.ToList();

            List<Parcel> newParcels = new();
            foreach(var parcel in parcels)
            {
                if (parcel is null) return;

                var srfFragments = parcel.Boundary.Split(cleanedDividers);

                var fragments = srfFragments
                    .Select(srf => new Parcel(srf, parcel.LandUse))
                    .ToList();

                newParcels.AddRange(fragments);
            }

            CoreModel.Parcels = newParcels;
        }

        private static void AddToCoreModel(
            List<ICurve> boundaries,
            List<(Landuse Use,Point Center)> priorUsesAndCenters)
        {
            var parcels = new List<Parcel>();

            foreach(var boundary in boundaries)
            {
                var srf = Create.PlanarSurface(boundary);
                Landuse use = AppLanduses.Undefined;

                if (priorUsesAndCenters.Count > 0)
                    use = priorUsesAndCenters
                        .MinBy(x => x.Center.Distance(boundary.ControlPointCentroid()))
                        .Use;

                if (srf != null)
                    parcels.Add(new Parcel(srf, use));
            }

            CoreModel.Parcels.AddRange(parcels);
        }

        private static void RemoveFromCoreModel(List<Guid> ids)
        {
            CoreModel.Parcels.RemoveAll(p => ids.Contains(p.CoreModelId));
        }
    }
}
