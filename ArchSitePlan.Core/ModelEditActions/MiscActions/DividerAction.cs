using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Models.CreationData;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.ModelEditActions.Misc
{
    public class DividerAction:IModelEditAction
    {
        public void Run(List<DividerData> dividerData,
           EditType editType)
        {
            switch (editType)
            {
                case EditType.Remove:
                    TryRemoveFromCoreModel(dividerData);
                    break;

                case EditType.Add or EditType.Edit:
                    TryRemoveFromCoreModel(dividerData);
                    TryAddToCoreModel(dividerData);
                    break;

                default:
                    throw new NotImplementedException(nameof(DividerAction) + $" Edit mode:{editType}");
            }
        }

        private static void TryAddToCoreModel(List<DividerData> dividersData)
        {
            var dividers = dividersData
                .Where(x => x.Curve != null)
                .Select(x => new ParcelDivider(x.Curve!, x.ID))
                .ToList();

            CoreModelSingelton.Instance.Dividers.AddRange(dividers);
        }

        private static void TryRemoveFromCoreModel(List<DividerData> dividers)
        {
            var ids = dividers.Select(x => x.ID).ToList();
            int count = CoreModelSingelton.Instance.Dividers.RemoveAll(d => ids.Contains(d.Id));
        }
    }
}
