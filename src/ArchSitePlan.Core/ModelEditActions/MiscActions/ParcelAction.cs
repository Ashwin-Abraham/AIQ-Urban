using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions.Misc;

public class ParcelAction:IModelEditAction
{
    public void Run(
        EditType editType,
        List<PlanarSurface>? parcelsToAdd = null,
        List<Guid>? clientIdsToModifyOrRemove = null,
        // use is only used during EditType.Edit
        string? useName = null,
        // dividers only used during EditType.Divide
        List<ICurve>? dividers = null,
        // existingUsesAndCenters only used during EditType.Add
        List<(Landuse, Point)>? existingUsesAndCenters = null)
    {
        var use = FindCoreModelUse(useName);

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
                    && parcelsToAdd != null)
                    RemoveFromCoreModel(clientIdsToModifyOrRemove);
                    AddToCoreModel(parcelsToAdd!, existingUsesAndCenters);
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

    private Landuse FindCoreModelUse(string? useName)
    {
        if (useName == null)
            return CoreModelSingelton.Instance.GetUndefinedUse();

        var use = CoreModelSingelton.Instance.Landuses.Find(x =>  x.Name == useName);
        return use == null? CoreModelSingelton.Instance.GetUndefinedUse() : use;
    }

    private static void ModifyLanduse(List<Guid> clientModelIDs, Landuse use)
    {
        CoreModelSingelton.Instance.Parcels.Where(p => clientModelIDs.Contains(p.ClientModelId))
            .ToList()
            .ForEach(p => p.LandUse = use);
    }

    private static void DivideInCoreModel(
        List<Guid> clientIds,
        List<ICurve> dividers)
    {
        var parcels = CoreModelSingelton.Instance.Parcels.Where(x => clientIds.Contains(x.ClientModelId)).ToList();

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

        CoreModelSingelton.Instance.Parcels = newParcels;
    }

    private static void AddToCoreModel(
        List<PlanarSurface> parcelSurfaces,
        List<(Landuse Use,Point Center)> priorUsesAndCenters)
    {
        List<Parcel> parcels = new();

        foreach(var srf in parcelSurfaces)
        {
            Landuse use = AppLanduses.Undefined;

            if (priorUsesAndCenters.Count > 0)
                use = priorUsesAndCenters
                    .MinBy(x => x.Center.Distance(srf.ExternalBoundary.ControlPointCentroid()))
                    .Use;

            parcels.Add(new Parcel(srf, use));
        }

        CoreModelSingelton.Instance.Parcels.AddRange(parcels);
    }

    private static void RemoveFromCoreModel(List<Guid> ids)
    {
        CoreModelSingelton.Instance.Parcels.RemoveAll(p => ids.Contains(p.CoreModelId));
    }
}
