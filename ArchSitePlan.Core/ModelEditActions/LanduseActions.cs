using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions;
public class LanduseActions
{
    static Landuse _undefined = CoreModelSingelton.Instance.GetUndefinedUse();

    public static void Run(
           List<LanduseData> landuseData,
           EditType editType)
    {
        var uses = landuseData.Select(x => x.ToLanduse()).ToList();

        switch (editType)
        {
            case EditType.Remove:
                ClearExceptUndefined(landuseData);
                break;

            case EditType.ClearAll:
                ClearAllExceptUndefined();
                break;

            case EditType.Edit or EditType.Add:
                uses.ForEach(x => ReplaceCoreModelUse(x));
                break;

            default:break;
        }
    }

    private static void ClearAllExceptUndefined()
    {
        var idsToClear = CoreModelSingelton.Instance.Landuses.Select(x => x.Id).ToList();
        ClearExceptUndefined(idsToClear);
    }

    private static void ClearExceptUndefined(List<LanduseData> data)
    {
        var idsToClear = data.Select(x => x.Id).ToList();
        ClearExceptUndefined(idsToClear);
    }
    /// <summary>
    /// Searches the core model for uses with the same guid.
    /// Then removes it replacing it with undefined.
    /// </summary>
    /// <param name="use"></param>
    private static void ClearExceptUndefined(List<Guid> idsToClear)
    {
        idsToClear = idsToClear.Except([_undefined.Id]).ToList();

        // Remove from other parts of model
        CoreModelSingelton.Instance.Parcels
            .Where(p => idsToClear.Contains(p.LandUse.Id))
            .ToList()
            .ForEach(x => x.LandUse = _undefined);

        CoreModelSingelton.Instance.Landuses.RemoveAll(x => idsToClear.Contains(x.Id));
    }

    /// <summary>
    /// Searches the core model for uses with the same guid. Then updates that use.
    /// If not found adds this use to the core model.
    /// </summary>
    /// <param name="use"></param>
    private static void ReplaceCoreModelUse(Landuse use)
    {
        if(use.Id == _undefined.Id) return;

        var existingUse = CoreModelSingelton.Instance.Landuses
            .FirstOrDefault(lu => lu.Id == use.Id);

        if (existingUse != null)
        {
            existingUse.Name = use.Name;
            existingUse.Color = use.Color;
        }
        else
        {
            CoreModelSingelton.Instance.Landuses.Add(use);
        }
    }
}
