using AIQ.Urban.Core.Models;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record BuildingUseChangeData(List<Guid> GeometryIds, Use NewUse) : ICreationData;
