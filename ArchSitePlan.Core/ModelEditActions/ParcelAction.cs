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
            List<(ICurve? boundary, Guid id)> boundaryAndIds,
            EditType editType,
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
                    TryRemoveFromCoreModel(GetIds(boundaryAndIds));
                    break;

                case EditType.Add:
                    TryRemoveFromCoreModel(GetIds(boundaryAndIds));
                    TryAddToCoreModel(boundaryAndIds, existingUsesAndCenters);
                    break;

                case EditType.Divide:
                    TryDivideInCoreModel(boundaryAndIds, dividers);
                    break;

                case EditType.Edit:
                    TryEditInCoreModel(boundaryAndIds, use);
                    break;

                default: 
                    throw new NotImplementedException(nameof(ParcelAction) + $" Edit mode:{editType}" );
            }
        }


        private bool TryEditInCoreModel(List<(ICurve boundary, Guid id)> boundaryAndIds, Landuse use)
        {
            var ids = boundaryAndIds.Select(x => x.id).ToList();

            CoreModel.Parcels.Where(p => ids.Contains(p.Id))
                .ToList()
                .ForEach(p => p.LandUse = use);
            
            return true;
        }

        private bool TryDivideInCoreModel(List<(ICurve? boundary, Guid id)> boundaryAndIds,
            List<ICurve?> dividers)
        {
            var ids = boundaryAndIds.Select(x => x.id).ToList();
            List<ICurve> cleanedDividers = dividers
                .Where(d => d is not null)
                .Cast<ICurve>()
                .ToList();

            List<Parcel> newParcels = new();
            foreach(var id in ids)
            {
                var parcel = CoreModel.Parcels.Find(p => p.Id == id);
                if (parcel is null) return false;

                var srfFragments = parcel.Boundary.Split(cleanedDividers);

                var parcels = srfFragments
                    .Select(srf => new Parcel(parcel, srf))
                    .ToList();

                newParcels.AddRange(parcels);
            }

            CoreModel.Parcels = newParcels;
            return ids.Count > 0;
        }

        private static bool TryAddToCoreModel(List<(ICurve boundary, Guid id)> boundaryAndIds,
            List<(Landuse Use,Point Center)> priorUsesAndCenters)
        {
            var parcels = new List<Parcel>();

            foreach(var boundaryId in boundaryAndIds)
            {
                var srf = Create.PlanarSurface(boundaryId.boundary);
                Landuse use = AppLanduses.Undefined;

                if (priorUsesAndCenters.Any())
                    use = priorUsesAndCenters
                        .MinBy(x => x.Center.Distance(boundaryId.boundary.ControlPointCentroid()))
                        .Use;

                if (srf != null)
                    parcels.Add(new Parcel(srf, use));
            }

            CoreModel.Parcels.AddRange(parcels);

            return parcels.Count > 0;
        }

        private static bool TryRemoveFromCoreModel(List<Guid> ids)
        {
            int count = CoreModel.Parcels.RemoveAll(p => ids.Contains(p.Id));
            return count > 0;
        }

        private static List<Guid> GetIds(List<(ICurve? obj, Guid id)> objectsAndIds)
        {
            return objectsAndIds.Select(x => x.id).ToList();
        }
    }
}
