using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

internal static class UrbanModelInputResolver
{
    internal static T? FindByGeometryId<T>(UrbanModel model, Guid geometryId)
        where T : class, IUrbanElement
    {
        return model.GetUrbanElements()
            .OfType<T>()
            .FirstOrDefault(x => x.GetGeometryIds().Contains(geometryId));
    }
}
