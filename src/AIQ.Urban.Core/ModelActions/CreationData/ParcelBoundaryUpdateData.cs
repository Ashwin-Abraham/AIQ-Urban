using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ParcelBoundaryUpdateData(Guid OldBoundaryId, Curve2D NewBoundary) : ICreationData;
