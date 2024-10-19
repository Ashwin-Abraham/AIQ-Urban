using ArchSitePlan.Core.Models;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions
{
    public class DividerAction:IModelEditAction
    {
        public void Run(List<(Polyline?, Guid)> dividers,
           EditType editType)
        {
            switch (editType)
            {
                case EditType.Remove:
                    TryRemoveFromCoreModel(dividers);
                    break;

                case EditType.Add or EditType.Edit:
                    TryRemoveFromCoreModel(dividers);
                    TryAddToCoreModel(dividers);
                    break;

                default:
                    throw new NotImplementedException(nameof(DividerAction) + $" Edit mode:{editType}");
            }
        }

        private static void TryAddToCoreModel(List<(Polyline? Curve, Guid ID)> dividersData)
        {
            var dividers = dividersData
                .Where(x => x.Curve != null)
                .Select(x => new ParcelDivider(x.Curve!, x.ID))
                .ToList();

            CoreModel.Dividers.AddRange(dividers);
        }

        private static void TryRemoveFromCoreModel(List<(Polyline? Curve, Guid ID)> dividers)
        {
            var ids = dividers.Select(x => x.ID).ToList();
            int count = CoreModel.Dividers.RemoveAll(d => ids.Contains(d.Id));
        }
    }
}
