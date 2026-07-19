using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ZoneUpdateData(Guid OldBoundaryId, Curve NewBoundary) : ICreationData;
