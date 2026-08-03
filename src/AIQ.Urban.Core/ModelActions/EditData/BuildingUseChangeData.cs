using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingUseChangeData(List<Guid> GeometryIds, Use NewUse) : IModelEditData;
