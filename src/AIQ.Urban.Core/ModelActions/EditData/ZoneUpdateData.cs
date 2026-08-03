using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record ZoneUpdateData(Guid OldBoundaryId, Curve NewBoundary) : IModelEditData;
