using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitUpdateData(Guid OldCenterlineId, Curve NewCenterline) : ICreationData;
