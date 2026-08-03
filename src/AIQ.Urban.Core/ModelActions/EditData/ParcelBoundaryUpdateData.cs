using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record ParcelBoundaryUpdateData(Guid OldBoundaryId, Curve2D NewBoundary) : IModelEditData;
