using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.ModelEditActions;
public static class DeleteBuildingAction
{
    public static int Run(List<Guid> envelopeIds)
    {
        return CoreModelSingelton.Instance.Buildings.RemoveAll(x => envelopeIds.Contains(x.EnvelopeId));
    }
}
